using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
/// <summary>
/// Výstupní DTO entity Form Definition response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>

public sealed record FormDefinitionResponse(
    Guid id,
    string name,
    string? description,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityProjection<FormDefinitionModel, FormDefinitionResponse>, IEntityMapper<FormDefinitionModel, FormDefinitionResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static FormDefinitionResponse FromModel(FormDefinitionModel model) => new(model.id, model.name, model.description, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<FormDefinitionModel, FormDefinitionResponse>> Projection => model => new(model.id, model.name, model.description, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public FormDefinitionModel ToModel() => new(id, name, description, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
