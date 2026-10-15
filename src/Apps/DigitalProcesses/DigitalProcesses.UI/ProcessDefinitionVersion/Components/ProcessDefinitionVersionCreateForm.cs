using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.ProcessDefinitionVersion.Components;

public static class ProcessDefinitionVersionCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page(
        "Create process definition version", Prefix + "/ProcessDefinitionVersionResponse/create-form",
        Prefix + "/ProcessDefinitionVersionResponse/list",
        "Choose a process, a form and workflow, then their published versions.",
        CreateFormFields.EntityLookup("process_definition_id", "Process", Prefix + "/ProcessDefinitionResponse/list", ["name"]),
        CreateFormFields.EntityLookup("form_definition_id", "Form", Prefix + "/FormDefinitionResponse/list", ["name"]),
        CreateFormFields.EntityLookup("form_version_id", "Published form version", Prefix + "/FormVersionResponse/list?where=%7B%22is_published%22%3A%7B%22_eq%22%3Atrue%7D%7D", [], parentField: "form_definition_id", parentProperty: "form_definition_id"),
        CreateFormFields.EntityLookup("workflow_definition_id", "Workflow", Prefix + "/WorkflowDefinitionResponse/list", ["name"]),
        CreateFormFields.EntityLookup("workflow_version_id", "Published workflow version", Prefix + "/WorkflowVersionResponse/list?where=%7B%22is_published%22%3A%7B%22_eq%22%3Atrue%7D%7D", [], parentField: "workflow_definition_id", parentProperty: "workflow_definition_id"),
        CreateFormFields.EntityLookup("target_group_type_id", "Allowed target group type", "/architecture/GroupTypeResponse/list", ["name"]),
        CreateFormFields.Check("publish", "Publish immediately"));
}
