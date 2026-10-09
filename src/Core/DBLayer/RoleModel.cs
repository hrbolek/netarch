using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

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
