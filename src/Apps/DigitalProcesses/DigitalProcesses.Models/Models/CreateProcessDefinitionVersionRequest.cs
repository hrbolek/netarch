namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateProcessDefinitionVersionRequest(
    Guid process_definition_id,
    Guid form_version_id,
    Guid workflow_version_id,
    int version,
    bool publish = false);
