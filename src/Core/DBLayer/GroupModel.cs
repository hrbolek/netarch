using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Uzel hierarchie skupin a RBAC objektů. Materializovaná cesta urychluje hledání předků a dědičnosti oprávnění; role a členství jsou samostatné vazby.
/// </summary>
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
    /// <summary>False if the organisational group is retired; historical RBAC rows stay intact.</summary>
    public bool valid { get; init; } = true;

    [NotMapped]
    public string? master_group_name { get; init; }

    [NotMapped]
    public string? group_type_name { get; init; }
}
