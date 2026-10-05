using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

public sealed record UserModel(
    Guid id,
    string Name,
    string? email,
    string? surname,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record GroupTypeModel(
    Guid id,
    string Name,
    Guid? parent_group_type_id,
    string path,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel
{
    [NotMapped]
    public string? parent_group_type_name { get; init; }
}

public sealed record GroupModel(
    Guid id,
    string Name,
    Guid group_type_id,
    Guid? master_group_id,
    string path,
    bool inherit_roles,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel
{
    [NotMapped]
    public string? master_group_name { get; init; }

    [NotMapped]
    public string? group_type_name { get; init; }
}

public sealed record MembershipModel(
    Guid id,
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel
{
    [NotMapped]
    public string? group_name { get; init; }

    [NotMapped]
    public string? user_name { get; init; }
}

public sealed record RoleModel(
    Guid id,
    Guid group_id,
    Guid user_id,
    Guid role_type_id,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid? delegated_by_role_id,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel
{
    [NotMapped]
    public string? group_name { get; init; }

    [NotMapped]
    public string? user_name { get; init; }

    public RoleTypeModel? role_type { get; init; }
}

public sealed record RoleTypeModel(
    Guid id,
    string name,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel
{
    public ICollection<RoleTypeOperationModel> operation_links { get; init; } = new List<RoleTypeOperationModel>();

    [NotMapped]
    public string[] requested_operations { get; init; } = Array.Empty<string>();
}

/// <summary>Catalog of authorization operations. New operations are data, not schema changes.</summary>
public sealed record OperationModel(
    Guid id,
    string name,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDBModel;

/// <summary>Normalized many-to-many mapping RoleType -> Operation.</summary>
public sealed record RoleTypeOperationModel(
    Guid id,
    Guid role_type_id,
    Guid operation_id
) : IEntityDBModel
{
    public OperationModel? operation { get; init; }
}


/// <summary>
/// Declarative mapping from a functional role valid at source_group_id to an operational
/// role over target_rbacobject_id. The binding contains no concrete user id.
/// SecurityProjectionService resolves current role holders into EffectiveAccess.
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
