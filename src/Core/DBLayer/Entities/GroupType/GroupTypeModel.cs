using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Klasifikace skupin umožňující oddělit organizační strukturu, RBAC objekty a další typy hierarchických uzlů.
/// </summary>

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
