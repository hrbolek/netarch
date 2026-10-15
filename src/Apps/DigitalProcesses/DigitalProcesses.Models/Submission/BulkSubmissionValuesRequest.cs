using System.Text.Json;
namespace WebAppArchitecture.DigitalProcesses.Models.Submission;
/// <summary>
/// Vstupní kontrakt Bulk Submission Values Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record BulkSubmissionValuesRequest(IReadOnlyList<BulkSubmissionValue> nodes);
