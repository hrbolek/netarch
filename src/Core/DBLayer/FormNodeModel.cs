using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Uzel stromu konkrétní verze formuláře; reprezentuje pole, sekci či zobrazovací prvek. Pořadí sourozenců je dáno pozicí, ne lexikografickým pořadím cesty.
/// </summary>
public sealed record FormNodeModel(
    Guid id,
    Guid form_version_id,
    Guid? parent_id,
    string path,
    string key,
    string? label,
    string? description,
    Guid node_type_id,
    int position,
    int min_occurs,
    int max_occurs,
    bool own_security_scope,
    string? configuration_json,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
