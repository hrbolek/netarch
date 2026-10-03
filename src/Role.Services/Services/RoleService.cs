using WebAppArchitecture.Base.Services;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Role.Services;

public interface IRoleService : IBaseService<RoleModel, RoleResponse> { }

public sealed class RoleService : BaseService<RoleModel, RoleResponse>, IRoleService
{
    private readonly ISecurityProjectionService _projection;

    public RoleService(ISecurityLayer security, ISecurityProjectionService projection)
        : base(security) => _projection = projection;

    public override Task<RoleResponse> CreateAsync<TCreateRequest>(TCreateRequest model, CancellationToken cancellationToken = default)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageCreateAsync(model.ToModel(), ct);
            await Security.SaveChangesAsync(ct); // source mutation, still inside transaction
            await _projection.RebuildAllAsync(ct);
            return RoleResponse.FromModel(entity);
        }, cancellationToken);

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
