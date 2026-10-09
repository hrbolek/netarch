namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record RequestDetailResponse(
    RequestResponse request,
    WorkflowStateResponse state,
    SubmissionTreeResponse active_submission,
    IReadOnlyCollection<RequestTransitionHistoryResponse> history);
