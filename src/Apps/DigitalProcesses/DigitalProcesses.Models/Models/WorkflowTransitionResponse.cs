using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Workflow Transition response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record WorkflowTransitionResponse(
    Guid id,
    Guid workflow_version_id,
    Guid source_state_id,
    Guid target_state_id,
    string key,
    string name,
    bool note_required,
    string? condition_query,
    string? action_query,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityProjection<WorkflowTransitionModel, WorkflowTransitionResponse>, IEntityMapper<WorkflowTransitionModel, WorkflowTransitionResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static WorkflowTransitionResponse FromModel(WorkflowTransitionModel model) => new(model.id, model.workflow_version_id, model.source_state_id, model.target_state_id, model.key, model.name, model.note_required, model.condition_query, model.action_query, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<WorkflowTransitionModel, WorkflowTransitionResponse>> Projection => model => new(model.id, model.workflow_version_id, model.source_state_id, model.target_state_id, model.key, model.name, model.note_required, model.condition_query, model.action_query, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public WorkflowTransitionModel ToModel() => new(id, workflow_version_id, source_state_id, target_state_id, key, name, note_required, condition_query, action_query, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
