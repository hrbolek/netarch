using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Internal authoring API for declarative RBAC bindings. Bindings are source data;
/// runtime authorization still reads only EffectiveAccess + RoleTypeOperation.
/// </summary>
public interface IRbacAccessBindingManager
{
    Task<RbacAccessBindingModel> StageCreateAsync(RbacAccessBindingModel binding, CancellationToken ct = default);
    Task ExpireManagedBindingsAsync(Guid managedScopeId, Guid targetRbacObjectId, DateTimeOffset at, Guid actor, CancellationToken ct = default);
}

public sealed class RbacAccessBindingManager(IDBLayer db) : IRbacAccessBindingManager
{
    public Task<RbacAccessBindingModel> StageCreateAsync(RbacAccessBindingModel binding, CancellationToken ct = default)
        => db.StageCreateAsync(binding, ct);

    public async Task ExpireManagedBindingsAsync(
        Guid managedScopeId,
        Guid targetRbacObjectId,
        DateTimeOffset at,
        Guid actor,
        CancellationToken ct = default)
    {
        var bindings = db.Query<RbacAccessBindingModel>()
            .Where(x => x.managed_scope_id == managedScopeId
                && x.target_rbacobject_id == targetRbacObjectId
                && (x.valid_to == null || x.valid_to > at))
            .ToArray();

        foreach (var binding in bindings)
            await db.StageUpdateAsync(binding with
            {
                valid_to = at,
                updated_at = at,
                updated_by = actor
            }, ct);
    }
}
