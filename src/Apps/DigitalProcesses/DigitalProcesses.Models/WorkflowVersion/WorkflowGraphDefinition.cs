using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
public sealed record WorkflowGraphDefinition(
    IReadOnlyList<WorkflowStateDefinition> states,
    IReadOnlyList<WorkflowTransitionDefinition> transitions);
