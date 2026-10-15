using System.Text.Json;
namespace WebAppArchitecture.DigitalProcesses.Models.Submission;
/// <summary>
/// Typ Bulk Submission Value ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed record BulkSubmissionValue(Guid id, string? value_json);
