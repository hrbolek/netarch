using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Request Transition History response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record RequestTransitionHistoryResponse(
    Guid id,
    Guid request_id,
    Guid transition_id,
    Guid from_state_id,
    Guid to_state_id,
    Guid archived_submission_id,
    Guid actor_user_id,
    string? note,
    DateTimeOffset changed_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<RequestTransitionHistoryModel, RequestTransitionHistoryResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static RequestTransitionHistoryResponse FromModel(RequestTransitionHistoryModel model) => new(model.id, model.request_id, model.transition_id, model.from_state_id, model.to_state_id, model.archived_submission_id, model.actor_user_id, model.note, model.changed_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Transition {model.changed_at:u}");
    public static Expression<Func<RequestTransitionHistoryModel, RequestTransitionHistoryResponse>> Projection => model => new(model.id, model.request_id, model.transition_id, model.from_state_id, model.to_state_id, model.archived_submission_id, model.actor_user_id, model.note, model.changed_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Transition");
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public RequestTransitionHistoryModel ToModel() => new(id, request_id, transition_id, from_state_id, to_state_id, archived_submission_id, actor_user_id, note, changed_at, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
