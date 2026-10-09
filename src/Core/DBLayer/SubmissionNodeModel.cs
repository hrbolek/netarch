using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

public sealed record SubmissionNodeModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;
