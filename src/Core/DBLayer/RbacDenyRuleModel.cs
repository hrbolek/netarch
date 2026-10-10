using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>Persistent deny on a role type in a Group subtree. The projection filters matching EffectiveAccess rows.</summary>
public sealed record RbacDenyRuleModel(
    Guid id,
    Guid group_id,
    Guid role_type_id,
    bool include_descendants,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
