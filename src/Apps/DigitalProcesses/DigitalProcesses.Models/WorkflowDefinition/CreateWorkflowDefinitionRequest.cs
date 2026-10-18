namespace WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
/// <summary>
/// Vstupní kontrakt Create Workflow Definition Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record CreateWorkflowDefinitionRequest(string name, string? description = null);
