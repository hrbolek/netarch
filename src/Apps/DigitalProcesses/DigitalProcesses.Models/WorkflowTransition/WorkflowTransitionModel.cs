using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Povolený přechod mezi dvěma stavy workflow; obchodní validace a kontrola oprávnění probíhají ve vrstvě Services.
/// </summary>
public sealed record WorkflowTransitionModel(
    Guid id,
    Guid workflow_version_id,
    Guid source_state_id,
    Guid target_state_id,
    string key,
    string name,
    bool note_required,
    string? condition_query,
    string? action_query,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
