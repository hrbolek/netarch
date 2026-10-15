using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.WorkflowTransition.Components;

public static class WorkflowTransitionCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page("Create workflow transition", Prefix+"/WorkflowTransitionResponse/create-form", Prefix+"/WorkflowTransitionResponse/list", "Adds a directed transition between two states of a draft workflow version.", CreateFormFields.GuidField("workflow_version_id","Workflow version ID"),CreateFormFields.GuidField("source_state_id","Source state ID"),CreateFormFields.GuidField("target_state_id","Target state ID"),CreateFormFields.Text("key","Key"),CreateFormFields.Text("name","Name"),CreateFormFields.Check("note_required","Transition note required"),CreateFormFields.TextArea("condition_query","Condition query",false),CreateFormFields.TextArea("action_query","Action query",false));
}
