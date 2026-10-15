namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
public sealed record WorkflowTransitionDefinition(
    string key, string source_key, string target_key, string name,
    bool note_required = false, string? condition_query = null, string? action_query = null);
