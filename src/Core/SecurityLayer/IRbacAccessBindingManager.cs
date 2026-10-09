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
