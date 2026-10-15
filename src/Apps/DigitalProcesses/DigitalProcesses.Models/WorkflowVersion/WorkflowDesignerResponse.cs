using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
/// <summary>
/// Výstupní DTO entity Workflow Designer response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record WorkflowDesignerResponse(
    WorkflowVersionResponse version,
    IReadOnlyCollection<WorkflowStateResponse> states,
    IReadOnlyCollection<WorkflowTransitionResponse> transitions);
