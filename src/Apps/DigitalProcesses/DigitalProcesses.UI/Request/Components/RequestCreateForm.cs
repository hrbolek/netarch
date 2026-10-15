using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.Request.Components;

public static class RequestCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page(
        "Create request", Prefix + "/RequestResponse/create-form", Prefix + "/RequestResponse/list",
        "Select a process and its published version. The acting user becomes the initiator.",
        CreateFormFields.EntityLookup("process_definition_id", "Process", Prefix + "/ProcessDefinitionResponse/list", ["name"], required: false),
        CreateFormFields.EntityLookup("process_definition_version_id", "Published process version (optional, latest published is used when empty)", Prefix + "/ProcessDefinitionVersionResponse/list?where=%7B%22is_published%22%3A%7B%22_eq%22%3Atrue%7D%7D", [], required: false, parentField: "process_definition_id", parentProperty: "process_definition_id"),
        CreateFormFields.EntityLookup("target_group_id", "Target authorization context (placeholder, defaults to the process group)", "/architecture/GroupResponse/list", ["Name"], required: false),
        CreateFormFields.Text("subject", "Subject", false));
}
