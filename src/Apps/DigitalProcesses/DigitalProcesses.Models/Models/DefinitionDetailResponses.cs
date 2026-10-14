namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record WorkflowDefinitionDetailResponse(
    WorkflowDefinitionResponse definition, IReadOnlyCollection<WorkflowVersionResponse> versions);

public sealed record ProcessDefinitionDetailResponse(
    ProcessDefinitionResponse definition, IReadOnlyCollection<ProcessDefinitionVersionResponse> versions);
