namespace WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
/// <summary>
/// Vstupní kontrakt Create Process Definition Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record CreateProcessDefinitionRequest(
    string name,
    Guid? initiator_role_type_id = null,
    string? description = null);
