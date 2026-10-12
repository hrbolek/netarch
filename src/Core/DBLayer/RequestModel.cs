using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Běhová instance žádosti vytvořená podle konkrétní verze procesu. Odděluje data uživatele a aktuální stav od verzovaných definic.
/// </summary>
public sealed record RequestModel(
    Guid id,
    Guid process_definition_version_id,
    Guid initiator_user_id,
    Guid current_state_id,
    Guid current_submission_id,
    string? subject,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
