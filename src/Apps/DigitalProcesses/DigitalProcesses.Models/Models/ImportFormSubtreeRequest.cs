using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Vstupní kontrakt Import Form Subtree Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record ImportFormSubtreeRequest(
    Guid source_node_id,
    Guid target_form_version_id,
    Guid? target_parent_id = null,
    string? key_prefix = null,
    int position = 0);
