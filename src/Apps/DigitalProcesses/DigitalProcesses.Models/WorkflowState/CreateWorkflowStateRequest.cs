namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
/// <summary>
/// Vstupní kontrakt Create Workflow State Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record CreateWorkflowStateRequest(
    Guid workflow_version_id,
    string key,
    string name,
    string? description = null,
    bool is_initial = false,
    bool is_terminal = false,
    int position = 0);
