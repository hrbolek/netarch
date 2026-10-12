using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Neměnná identita konkrétní verze procesu. Spojuje verzi formuláře a workflow, uchovává pořadové číslo a příznak publikování; žádosti jsou navázány na konkrétní verzi.
/// </summary>
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
) : ISecuredEntityDBModel
{
    /// <summary>Published versions restrict new requests to this exact target group type.</summary>
    public Guid? target_group_type_id { get; init; }
}
