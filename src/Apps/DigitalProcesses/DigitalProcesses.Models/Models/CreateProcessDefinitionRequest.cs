namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateProcessDefinitionRequest(
    string name,
    Guid? initiator_role_type_id = null,
    string? description = null);
