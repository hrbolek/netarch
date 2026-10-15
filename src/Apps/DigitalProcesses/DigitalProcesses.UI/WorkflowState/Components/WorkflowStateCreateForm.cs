using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.WorkflowState.Components;

public static class WorkflowStateCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page("Create workflow state", Prefix+"/WorkflowStateResponse/create-form", Prefix+"/WorkflowStateResponse/list", "Adds a state to a draft workflow version.", CreateFormFields.GuidField("workflow_version_id","Workflow version ID"),CreateFormFields.Text("key","Key"),CreateFormFields.Text("name","Name"),CreateFormFields.TextArea("description","Description",false),CreateFormFields.Check("is_initial","Initial state"),CreateFormFields.Check("is_terminal","Terminal state"),CreateFormFields.Number("position","Position","0"));
}
