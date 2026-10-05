using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Services;

public interface IGroupService : IBaseService<GroupModel, GroupResponse> { }

public sealed class GroupService : BaseService<GroupModel, GroupResponse>, IGroupService
{
    private readonly ISecurityProjectionService _projection;

    public GroupService(ISecurityLayer security, ISecurityProjectionService projection)
        : base(security) => _projection = projection;

    public override Task<GroupResponse> CreateAsync<TCreateRequest>(TCreateRequest model, CancellationToken cancellationToken = default)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageCreateAsync(model.ToModel(), ct);
            await Security.SaveChangesAsync(ct);
            await _projection.RebuildAllAsync(ct);
            return GroupResponse.FromModel(entity);
        }, cancellationToken);

    public override async Task<GroupResponse> UpdateAsync<TUpdateRequest>(TUpdateRequest model, CancellationToken cancellationToken = default)
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
                await _projection.RebuildAllAsync(ct);
            return GroupResponse.FromModel(entity);
        }, cancellationToken);
    }

    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            await Security.StageDeleteAsync<GroupModel>(id, ct);
            await Security.SaveChangesAsync(ct);
            await _projection.RebuildAllAsync(ct);
        }, cancellationToken);
}

public interface IGroupTypeService : IBaseService<GroupTypeModel, GroupTypeResponse> { }

public sealed class GroupTypeService : BaseService<GroupTypeModel, GroupTypeResponse>, IGroupTypeService
{
    public GroupTypeService(ISecurityLayer security) : base(security) { }
}
