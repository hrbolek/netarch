using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Role.Services;

public interface IRoleService : IBaseService<RoleModel, RoleResponse>
{
    Task<RoleResponse> CreateContainedAsync(RoleCreateContainedRequest request, CancellationToken cancellationToken = default);
}

public sealed class RoleService : BaseService<RoleModel, RoleResponse>, IRoleService
{
    private readonly ISecurityProjectionService _projection;

    public RoleService(ISecurityLayer security, ISecurityProjectionService projection)
        : base(security) => _projection = projection;

    public async Task<RoleResponse> CreateContainedAsync(
        RoleCreateContainedRequest request,
        CancellationToken cancellationToken = default)
    {
        var (group, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, request.group_id, "contained", cancellationToken);
        _ = await Security.GetByIdAsync<UserModel>(request.user_id, SecurityOperations.Read, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");
        _ = await Security.GetByIdAsync<RoleTypeModel>(request.role_type_id, SecurityOperations.Read, cancellationToken)
            ?? throw new KeyNotFoundException("Role type not found.");

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new RoleModel(
            Guid.NewGuid(), group.id, request.user_id, request.role_type_id,
            request.is_deputy, request.valid_from, request.valid_to, request.delegated_by_role_id,
            group.rbacobject_id, now, now, actor, actor);

        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageCreateAsync(model, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.RebuildAllAsync(ct);
            return RoleResponse.FromModel(entity);
        }, cancellationToken);
    }

    public override async Task<RoleResponse> UpdateAsync<TUpdateRequest>(TUpdateRequest model, CancellationToken cancellationToken = default)
    {
        var current = await Security.GetByIdAsync<RoleModel>(model.id, SecurityOperations.Update, cancellationToken)
            ?? throw new InvalidOperationException($"Item with id '{model.id}' does not exist or is not accessible.");
        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageUpdateAsync(model.ApplyTo(current), ct);
            await Security.SaveChangesAsync(ct);
            await _projection.RebuildAllAsync(ct);
            return RoleResponse.FromModel(entity);
        }, cancellationToken);
    }

    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            await Security.StageDeleteAsync<RoleModel>(id, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.RebuildAllAsync(ct);
        }, cancellationToken);
}
