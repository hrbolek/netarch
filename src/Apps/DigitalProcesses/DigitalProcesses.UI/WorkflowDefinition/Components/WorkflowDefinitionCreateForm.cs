using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.WorkflowDefinition.Components;

public static class WorkflowDefinitionCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page("Create workflow definition", Prefix+"/WorkflowDefinitionResponse/create-form", Prefix+"/WorkflowDefinitionResponse/list", "Creates the stable identity of a workflow.", CreateFormFields.Text("name","Name"),CreateFormFields.TextArea("description","Description",false));
}
