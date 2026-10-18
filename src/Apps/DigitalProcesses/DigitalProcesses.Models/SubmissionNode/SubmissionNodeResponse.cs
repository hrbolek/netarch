using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
/// <summary>
/// Výstupní DTO entity Submission Node response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>

public sealed record SubmissionNodeResponse(
    Guid id,
    Guid submission_id,
    Guid form_node_id,
    Guid? parent_id,
    string path,
    int occurrence,
    int position,
    string? value_json,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityProjection<SubmissionNodeModel, SubmissionNodeResponse>, IEntityMapper<SubmissionNodeModel, SubmissionNodeResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static SubmissionNodeResponse FromModel(SubmissionNodeModel model) => new(model.id, model.submission_id, model.form_node_id, model.parent_id, model.path, model.occurrence, model.position, model.value_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Node {model.occurrence}");
    public static Expression<Func<SubmissionNodeModel, SubmissionNodeResponse>> Projection => model => new(model.id, model.submission_id, model.form_node_id, model.parent_id, model.path, model.occurrence, model.position, model.value_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Node");
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public SubmissionNodeModel ToModel() => new(id, submission_id, form_node_id, parent_id, path, occurrence, position, value_json, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
