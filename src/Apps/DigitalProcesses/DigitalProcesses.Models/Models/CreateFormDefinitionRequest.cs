namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateFormDefinitionRequest(string name, string? description = null);
