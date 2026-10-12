using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Definice druhu role a jejího oprávnění k operacím; konkrétní přidělení role se odkazuje na tento typ.
/// </summary>
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
