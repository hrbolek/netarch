using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.RoleType.Models;

/// <summary>
/// Výstupní DTO entity Role Type response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record RoleTypeResponse(
    Guid id,
    string name,
    string[] operations,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<RoleTypeModel, RoleTypeResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static RoleTypeResponse FromModel(RoleTypeModel model)
        => new(model.id, model.name,
            model.operation_links.Where(x => x.operation is not null).Select(x => x.operation!.name).OrderBy(x => x).ToArray(),
            model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);

    public static Expression<Func<RoleTypeModel, RoleTypeResponse>> Projection => model => new RoleTypeResponse(
        model.id, model.name,
        model.operation_links.Where(x => x.operation != null).Select(x => x.operation!.name).OrderBy(x => x).ToArray(),
        model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public RoleTypeModel ToModel() => new(id, name, rbacobject_id, created_at, updated_at, created_by, updated_by)
    {
        requested_operations = operations
    };
}
