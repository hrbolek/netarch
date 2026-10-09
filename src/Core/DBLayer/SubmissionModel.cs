using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

public sealed record SubmissionModel(
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
    Guid updated_by
) : ISecuredEntityDBModel;
