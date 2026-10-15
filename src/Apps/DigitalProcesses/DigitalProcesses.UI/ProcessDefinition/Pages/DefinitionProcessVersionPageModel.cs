using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.ProcessDefinition.Pages;

public sealed record DefinitionProcessVersionPageModel(
    Guid Id,
    string Name,
    IReadOnlyList<DefinitionVersionItem> Versions,
    ProcessDefinitionResponse? ProcessDefinition = null)
{
    public string Kind => "Process";
}
