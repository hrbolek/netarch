using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
public sealed record WorkflowDefinitionDetailResponse(
    WorkflowDefinitionResponse definition, IReadOnlyCollection<WorkflowVersionResponse> versions);
