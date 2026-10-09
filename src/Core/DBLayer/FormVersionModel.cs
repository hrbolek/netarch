using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

public sealed record FormVersionModel(
    Guid id,
    Guid form_definition_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
