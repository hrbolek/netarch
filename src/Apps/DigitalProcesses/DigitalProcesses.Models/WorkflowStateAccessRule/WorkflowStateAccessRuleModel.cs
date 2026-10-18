using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowStateAccessRule;

// ============================================================
// Digital Processes - definition models
// ============================================================

/// <summary>
/// Typ Workflow State Access Rule ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>

public sealed record WorkflowStateAccessRuleModel(
    Guid id,
    Guid workflow_state_id,
    Guid source_role_type_id,
    Guid target_role_type_id,
    string target_scope,
    Guid? target_form_node_id,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

// ============================================================
// Digital Processes - runtime models
// ============================================================
