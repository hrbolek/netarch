namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateWorkflowDefinitionRequest(string name, string? description = null);
