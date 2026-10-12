using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Workflow Version response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record WorkflowVersionResponse(
    Guid id,
    Guid workflow_definition_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<WorkflowVersionModel, WorkflowVersionResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static WorkflowVersionResponse FromModel(WorkflowVersionModel model) => new(model.id, model.workflow_definition_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Workflow version {model.version}");
    public static Expression<Func<WorkflowVersionModel, WorkflowVersionResponse>> Projection => model => new(model.id, model.workflow_definition_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Workflow version");
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public WorkflowVersionModel ToModel() => new(id, workflow_definition_id, version, is_published, published_at, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
