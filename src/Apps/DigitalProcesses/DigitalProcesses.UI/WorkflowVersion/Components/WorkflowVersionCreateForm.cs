using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.WorkflowVersion.Components;

public static class WorkflowVersionCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page("Create workflow version", Prefix+"/WorkflowVersionResponse/create-form", Prefix+"/WorkflowVersionResponse/list", "Creates a draft version of a workflow definition.", CreateFormFields.GuidField("workflow_definition_id","Workflow definition ID"),CreateFormFields.Number("version","Version","1","1"),CreateFormFields.Check("publish","Publish immediately"));
}
