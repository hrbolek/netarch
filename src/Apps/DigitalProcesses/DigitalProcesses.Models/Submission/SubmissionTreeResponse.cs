using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
namespace WebAppArchitecture.DigitalProcesses.Models.Submission;
/// <summary>
/// Výstupní DTO entity Submission Tree response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record SubmissionTreeResponse(SubmissionResponse submission, IReadOnlyCollection<SubmissionNodeResponse> nodes);
