using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.WorkflowDefinition.Pages;

public sealed record DefinitionWorkflowVersionPageModel(
    Guid Id,
    string Name,
    IReadOnlyList<DefinitionVersionItem> Versions,
    WorkflowDefinitionResponse? WorkflowDefinition = null)
{
    public string Kind => "Workflow";
}
