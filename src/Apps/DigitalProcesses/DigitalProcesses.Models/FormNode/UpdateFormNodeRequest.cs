namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
/// <summary>
/// Vstupní kontrakt Update Form Node Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record UpdateFormNodeRequest(
    string key,
    string? label = null,
    string? description = null,
    string? configuration_json = null,
    int min_occurs = 1,
    int max_occurs = 1);
