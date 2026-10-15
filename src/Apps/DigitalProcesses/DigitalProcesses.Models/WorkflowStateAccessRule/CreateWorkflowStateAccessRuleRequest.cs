namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowStateAccessRule;
/// <summary>
/// Vstupní kontrakt Create Workflow State Access Rule Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record CreateWorkflowStateAccessRuleRequest(
    Guid workflow_state_id,
    Guid source_role_type_id,
    Guid target_role_type_id,
    string target_scope,
    Guid? target_form_node_id = null);
