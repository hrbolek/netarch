using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

public sealed record ProcessDefinitionVersionModel(
    Guid id,
    Guid process_definition_id,
    Guid form_version_id,
    Guid workflow_version_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
