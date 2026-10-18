using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.AuthorizationLayer;

/// <summary>
/// Internal authoring API for declarative RBAC bindings. Bindings are source data;
/// runtime authorization still reads only EffectiveAccess + RoleTypeOperation.
/// </summary>
/// <summary>
/// Typ Rbac Access Binding Manager ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed class RbacAccessBindingManager(IDBLayer db) : IRbacAccessBindingManager
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: StageCreateAsync, Query; pořadí a parametry viz implementace.
    /// </summary>
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
