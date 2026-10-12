using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Submission response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record SubmissionResponse(
    Guid id,
    Guid request_id,
    Guid form_version_id,
    int version,
    bool is_archived,
    DateTimeOffset? archived_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<SubmissionModel, SubmissionResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static SubmissionResponse FromModel(SubmissionModel model) => new(model.id, model.request_id, model.form_version_id, model.version, model.is_archived, model.archived_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Submission v{model.version}");
    public static Expression<Func<SubmissionModel, SubmissionResponse>> Projection => model => new(model.id, model.request_id, model.form_version_id, model.version, model.is_archived, model.archived_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Submission");
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public SubmissionModel ToModel() => new(id, request_id, form_version_id, version, is_archived, archived_at, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
