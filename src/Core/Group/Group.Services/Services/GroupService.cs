using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Services;

/// <summary>
/// Služba Group Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class GroupService : SecuredBaseService<GroupModel>, IGroupService
{
    private readonly ISecurityProjectionService _projection;

    public GroupService(ISecurityLayer security, IDBLayer unsecuredDb, ISecurityProjectionService projection)
        : base(security, unsecuredDb) => _projection = projection;

    public async Task<GroupResponse> CreateContainedAsync(
        GroupCreateContainedRequest request,
        CancellationToken cancellationToken = default)
    {
        var (parent, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, request.master_group_id, "contained", cancellationToken);
        var groupTypeId = request.group_type_id ?? parent.group_type_id;
        _ = await Security.GetByIdAsync<GroupTypeModel>(groupTypeId, SecurityOperations.Read, cancellationToken)
            ?? throw new KeyNotFoundException("Group type not found.");
        return await CreateCoreAsync(request.Name, groupTypeId, parent, request.inherit_roles, cancellationToken);
    }

    public async Task<GroupResponse> CreateTypedAsync(
        GroupCreateTypedRequest request,
        CancellationToken cancellationToken = default)
    {
        var (groupType, _) = await CreationScopeGuard.RequireEntityAsync<GroupTypeModel>(
            Security, request.group_type_id, "typed", cancellationToken);
        return await CreateCoreAsync(request.Name, groupType.id, null, request.inherit_roles, cancellationToken);
    }

    private Task<GroupResponse> CreateCoreAsync(
        string name,
        Guid groupTypeId,
        GroupModel? parent,
        bool inheritRoles,
        CancellationToken cancellationToken)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            var id = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var actor = Security.CurrentUserId;
            var path = parent is null ? $"/{id:D}/" : $"{parent.path}{id:D}/";
            var model = new GroupModel(
                id, name, groupTypeId, parent?.id, path, inheritRoles, id,
                now, now, actor, actor);
            var entity = await Security.StageCreateAsync(model, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.SynchronizeRbacObjectAsync(entity.id, ct);
            return GroupResponse.FromModel(entity);
        }, cancellationToken);

    public override async Task<TDTO> UpdateAsync<TUpdateRequest, TDTO>(TUpdateRequest model, CancellationToken cancellationToken = default)
    {
        var current = await Security.GetByIdAsync<GroupModel>(model.id, SecurityOperations.Update, cancellationToken)
            ?? throw new InvalidOperationException($"Item with id '{model.id}' does not exist or is not accessible.");

        var updatedModel = model.ApplyTo(current);
        var hierarchyChanged = current.master_group_id != updatedModel.master_group_id
            || current.inherit_roles != updatedModel.inherit_roles;

        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageUpdateAsync(updatedModel, ct);
            await Security.SaveChangesAsync(ct);
            if (hierarchyChanged)
                await _projection.SynchronizeGroupSubtreeAsync(entity.id, ct);
            return TDTO.FromModel(entity);
        }, cancellationToken);
    }

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: GetByIdAsync, ExecuteInTransactionAsync, StageDeleteAsync, SaveChangesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public override async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await Security.GetByIdAsync<GroupModel>(id, SecurityOperations.Delete, cancellationToken);
        if (current is null)
            return;

        await Security.ExecuteInTransactionAsync(async ct =>
        {
            await Security.StageDeleteAsync<GroupModel>(id, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.SynchronizeGroupSubtreeAsync(current.id, ct);
        }, cancellationToken);
    }
}
