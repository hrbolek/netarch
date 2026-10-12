namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Vstupní kontrakt Create Form Node Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record CreateFormNodeRequest(
    Guid form_version_id,
    string key,
    Guid node_type_id,
    Guid? parent_id = null,
    string? label = null,
    string? description = null,
    int position = 0,
    int min_occurs = 1,
    int max_occurs = 1,
    bool own_security_scope = false,
    string? configuration_json = null);
