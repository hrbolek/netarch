using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Workflow State response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record WorkflowStateResponse(
    Guid id,
    Guid workflow_version_id,
    string key,
    string name,
    string? description,
    bool is_initial,
    bool is_terminal,
    int position,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<WorkflowStateModel, WorkflowStateResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static WorkflowStateResponse FromModel(WorkflowStateModel model) => new(model.id, model.workflow_version_id, model.key, model.name, model.description, model.is_initial, model.is_terminal, model.position, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<WorkflowStateModel, WorkflowStateResponse>> Projection => model => new(model.id, model.workflow_version_id, model.key, model.name, model.description, model.is_initial, model.is_terminal, model.position, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public WorkflowStateModel ToModel() => new(id, workflow_version_id, key, name, description, is_initial, is_terminal, position, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
