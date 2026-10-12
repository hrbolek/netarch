using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Workflow Definition response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record WorkflowDefinitionResponse(
    Guid id,
    string name,
    string? description,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<WorkflowDefinitionModel, WorkflowDefinitionResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static WorkflowDefinitionResponse FromModel(WorkflowDefinitionModel model) => new(model.id, model.name, model.description, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<WorkflowDefinitionModel, WorkflowDefinitionResponse>> Projection => model => new(model.id, model.name, model.description, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public WorkflowDefinitionModel ToModel() => new(id, name, description, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
