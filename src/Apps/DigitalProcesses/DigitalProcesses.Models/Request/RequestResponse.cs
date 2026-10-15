using System.Linq.Expressions;
using System.Text.Json.Serialization;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.Request;
/// <summary>
/// Výstupní DTO entity Request response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record RequestResponse(
    Guid id,
    Guid process_definition_version_id,
    Guid initiator_user_id,
    Guid current_state_id,
    Guid current_submission_id,
    string? subject,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityProjection<RequestModel, RequestResponse>, IEntityMapper<RequestModel, RequestResponse>, ISecuredEntityDTOModel
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RequestWorkspaceResponse? workspace { get; init; }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static RequestResponse FromModel(RequestModel model) => new(model.id, model.process_definition_version_id, model.initiator_user_id, model.current_state_id, model.current_submission_id, model.subject, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, (model.subject ?? $"Request {model.id}"));
    public static Expression<Func<RequestModel, RequestResponse>> Projection => model => new(model.id, model.process_definition_version_id, model.initiator_user_id, model.current_state_id, model.current_submission_id, model.subject, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, (model.subject ?? "Request"));
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public RequestModel ToModel() => new(id, process_definition_version_id, initiator_user_id, current_state_id, current_submission_id, subject, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
