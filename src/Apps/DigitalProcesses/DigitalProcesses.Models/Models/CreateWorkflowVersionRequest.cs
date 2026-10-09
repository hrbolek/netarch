namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateWorkflowVersionRequest(Guid workflow_definition_id, int version, bool publish = false);
