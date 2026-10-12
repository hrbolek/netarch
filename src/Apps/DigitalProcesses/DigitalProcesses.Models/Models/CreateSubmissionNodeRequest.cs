using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Vstupní kontrakt Create Submission Node Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record CreateSubmissionNodeRequest(
    Guid submission_id,
    Guid form_node_id,
    Guid? parent_id = null,
    int occurrence = 0,
    int position = 0,
    string? value_json = null);
