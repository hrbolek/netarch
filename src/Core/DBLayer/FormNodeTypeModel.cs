using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

public sealed record FormNodeTypeModel(
    Guid id,
    Guid? parent_type_id,
    string name,
    string renderer_key,
    bool can_have_children,
    bool can_have_value,
    bool can_repeat,
    string? configuration_schema_json,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
