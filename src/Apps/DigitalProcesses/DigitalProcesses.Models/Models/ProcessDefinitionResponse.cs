using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Process Definition response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record ProcessDefinitionResponse(
    Guid id,
    string name,
    string? description,
    Guid initiator_role_type_id,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityProjection<ProcessDefinitionModel, ProcessDefinitionResponse>, IEntityMapper<ProcessDefinitionModel, ProcessDefinitionResponse>, ISecuredEntityDTOModel
{
    public Guid? form_definition_id { get; init; }
    public Guid? workflow_definition_id { get; init; }
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static ProcessDefinitionResponse FromModel(ProcessDefinitionModel model) => new(model.id, model.name, model.description, model.initiator_role_type_id, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name) { form_definition_id = model.form_definition_id, workflow_definition_id = model.workflow_definition_id };
    public static Expression<Func<ProcessDefinitionModel, ProcessDefinitionResponse>> Projection => model => new(model.id, model.name, model.description, model.initiator_role_type_id, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name) { form_definition_id = model.form_definition_id, workflow_definition_id = model.workflow_definition_id };
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public ProcessDefinitionModel ToModel() => new(id, name, description, initiator_role_type_id, rbacobject_id, created_at, updated_at, created_by, updated_by) { form_definition_id = form_definition_id, workflow_definition_id = workflow_definition_id };
}
