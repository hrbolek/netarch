using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

public sealed record RequestTransitionHistoryModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;
