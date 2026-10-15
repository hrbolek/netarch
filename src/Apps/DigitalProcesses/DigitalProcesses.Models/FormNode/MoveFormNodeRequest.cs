namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
/// <summary>
/// Vstupní kontrakt Move Form Node Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record MoveFormNodeRequest(Guid? parent_id, int position = 0);
