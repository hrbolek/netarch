using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Vstupní kontrakt Set Submission Node Value Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record SetSubmissionNodeValueRequest(string? value_json);
