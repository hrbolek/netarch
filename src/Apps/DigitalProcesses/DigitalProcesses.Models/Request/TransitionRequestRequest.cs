namespace WebAppArchitecture.DigitalProcesses.Models.Request;
/// <summary>
/// Vstupní kontrakt Transition Request Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record TransitionRequestRequest(Guid transition_id, string? note = null);
