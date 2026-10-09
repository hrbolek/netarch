using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

public sealed record WorkflowStateModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;
