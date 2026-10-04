using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Materialized authorization projection. Derived from Role + Group hierarchy and,
/// for user security objects, from Membership.
/// </summary>
/// <remarks>
/// Validity is stored as Unix milliseconds so SQLite can translate range comparisons.
/// source_membership_id is Guid.Empty for group-derived rows and contains the membership
/// id for rows projected to User security objects. Runtime security queries do not depend
/// on the provenance fields.
/// </remarks>
public sealed record EffectiveAccessModel(
    Guid user_id,
    Guid rbacobject_id,
    Guid role_type_id,
    Guid source_role_id,
    Guid source_group_id,
    Guid source_membership_id,
    long valid_from_unix_ms,
    long? valid_to_unix_ms
) : IEntityDBModel
{
    public Guid id => source_role_id;
}
