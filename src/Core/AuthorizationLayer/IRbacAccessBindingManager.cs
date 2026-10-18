using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.AuthorizationLayer;

/// <summary>
/// Internal authoring API for declarative RBAC bindings. Bindings are source data;
/// runtime authorization still reads only EffectiveAccess + RoleTypeOperation.
/// </summary>
/// <summary>
/// Typ I Rbac Access Binding Manager ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public interface IRbacAccessBindingManager
{
    Task<RbacAccessBindingModel> StageCreateAsync(RbacAccessBindingModel binding, CancellationToken ct = default);
    Task ExpireManagedBindingsAsync(Guid managedScopeId, Guid targetRbacObjectId, DateTimeOffset at, Guid actor, CancellationToken ct = default);
}
