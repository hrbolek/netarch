namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateWorkflowStateRequest(
    Guid workflow_version_id,
    string key,
    string name,
    string? description = null,
    bool is_initial = false,
    bool is_terminal = false,
    int position = 0);
