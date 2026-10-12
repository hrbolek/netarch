using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Materialized authorization projection. This is the single runtime source used by SecurityLayer.
/// Rows may originate from a direct Role/group-tree projection, Membership projection, or an
/// RbacAccessBinding that maps a functional role at one security scope to an operational role
/// at another RBAC object.
/// </summary>
/// <summary>
/// Projekce efektivních oprávnění. Je odvozená z RBAC vazeb a není nezávislým zdrojem pravdy.
/// </summary>
public sealed record EffectiveAccessModel(
    Guid user_id,
    Guid rbacobject_id,
    Guid role_type_id,
    Guid source_role_id,
    Guid source_group_id,
    Guid source_membership_id,
    Guid source_access_binding_id,
    long valid_from_unix_ms,
    long? valid_to_unix_ms
) : IEntityDBModel
{
    public Guid id => source_access_binding_id == Guid.Empty ? source_role_id : source_access_binding_id;
}
