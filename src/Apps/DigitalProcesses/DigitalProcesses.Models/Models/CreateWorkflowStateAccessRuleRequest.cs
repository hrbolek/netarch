namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateWorkflowStateAccessRuleRequest(
    Guid workflow_state_id,
    Guid source_role_type_id,
    Guid target_role_type_id,
    string target_scope,
    Guid? target_form_node_id = null);
