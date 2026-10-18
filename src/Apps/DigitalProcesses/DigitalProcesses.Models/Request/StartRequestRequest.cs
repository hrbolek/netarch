namespace WebAppArchitecture.DigitalProcesses.Models.Request;
/// <summary>
/// Vstupní kontrakt Start Request Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record StartRequestRequest(
    Guid process_definition_version_id,
    string? subject = null,
    Guid? target_group_id = null,
    Guid? process_definition_id = null);
