using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Typ Rbac Access Binding ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>

public sealed record RbacAccessBindingModel(
    Guid id,
    Guid source_group_id,
    Guid source_role_type_id,
    Guid target_rbacobject_id,
    Guid target_role_type_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid? managed_scope_id,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
