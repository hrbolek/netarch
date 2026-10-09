namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record WorkflowDesignerResponse(
    WorkflowVersionResponse version,
    IReadOnlyCollection<WorkflowStateResponse> states,
    IReadOnlyCollection<WorkflowTransitionResponse> transitions);
