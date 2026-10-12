using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Request Detail response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record RequestDetailResponse(
    RequestResponse request,
    WorkflowStateResponse state,
    SubmissionTreeResponse active_submission,
    IReadOnlyCollection<RequestTransitionHistoryResponse> history);
