namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
public sealed record WorkflowStateDefinition(
    string key, string name, string? description = null,
    bool is_initial = false, bool is_terminal = false, int position = 0);
