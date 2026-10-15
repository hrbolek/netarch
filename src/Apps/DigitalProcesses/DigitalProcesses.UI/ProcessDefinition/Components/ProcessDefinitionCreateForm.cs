using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.ProcessDefinition.Components;

public static class ProcessDefinitionCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page(
        "Create process definition",
        Prefix + "/ProcessDefinitionResponse/create-form",
        Prefix + "/ProcessDefinitionResponse/list",
        "Creates a process together with its own form, workflow, and first draft versions. The request initiator uses the system role type 'já'.",
        CreateFormFields.Text("name", "Name"),
        CreateFormFields.TextArea("description", "Description", false));
}
