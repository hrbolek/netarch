namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record SubmissionTreeResponse(SubmissionResponse submission, IReadOnlyCollection<SubmissionNodeResponse> nodes);
