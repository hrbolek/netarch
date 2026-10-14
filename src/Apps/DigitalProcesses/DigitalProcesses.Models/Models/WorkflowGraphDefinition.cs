namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record WorkflowGraphDefinition(
    IReadOnlyList<WorkflowStateDefinition> states,
    IReadOnlyList<WorkflowTransitionDefinition> transitions);

public sealed record WorkflowStateDefinition(
    string key, string name, string? description = null,
    bool is_initial = false, bool is_terminal = false, int position = 0);

public sealed record WorkflowTransitionDefinition(
    string key, string source_key, string target_key, string name,
    bool note_required = false, string? condition_query = null, string? action_query = null);
