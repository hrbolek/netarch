using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Vstupní kontrakt Create Process Definition Version Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record CreateProcessDefinitionVersionRequest(
    Guid process_definition_id,
    Guid form_version_id,
    Guid workflow_version_id,
    int version,
    bool publish = false,
    Guid? target_group_type_id = null);
