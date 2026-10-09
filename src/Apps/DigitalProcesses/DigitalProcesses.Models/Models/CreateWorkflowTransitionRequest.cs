namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateWorkflowTransitionRequest(
    Guid workflow_version_id,
    Guid source_state_id,
    Guid target_state_id,
    string key,
    string name,
    bool note_required = false,
    string? condition_query = null,
    string? action_query = null);
