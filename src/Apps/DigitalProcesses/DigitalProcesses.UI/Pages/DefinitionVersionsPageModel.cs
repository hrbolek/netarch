namespace WebAppArchitecture.DigitalProcesses.UI.Pages;
public sealed record DefinitionVersionsPageModel(Guid Id, string Name, string Kind, IReadOnlyList<DefinitionVersionItem> Versions, WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinitionResponse? WorkflowDefinition = null, WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionResponse? ProcessDefinition = null);
