using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

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
) : ISecuredEntityDBModel, IEntityWithTree
{
    [NotMapped]
    public string? master_group_name { get; init; }

    [NotMapped]
    public string? group_type_name { get; init; }
}
