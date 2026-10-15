using WebAppArchitecture.DigitalProcesses.Models.RequestTransitionHistory;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
namespace WebAppArchitecture.DigitalProcesses.Models.Request;
/// <summary>
/// Výstupní DTO entity Request Detail response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record RequestDetailResponse(
    RequestResponse request,
    WorkflowStateResponse state,
    SubmissionTreeResponse active_submission,
    IReadOnlyCollection<RequestTransitionHistoryResponse> history);
