using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>Materialized authorization projection. Derived from Role + Group hierarchy.</summary>
public sealed record EffectiveAccessModel(
    Guid user_id,
    Guid rbac_id,
    Guid role_type_id,
    Guid source_role_id,
    Guid source_group_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to
) : IEntityDBModel
{
    public Guid id => source_role_id;
}
