namespace WebAppArchitecture.DigitalProcesses.Models.FormVersion;
/// <summary>
/// Vstupní kontrakt Create Form Version Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record CreateFormVersionRequest(Guid form_definition_id, int version, bool publish = false);
