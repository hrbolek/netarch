using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.WorkflowStateAccessRule.Components;

public static class WorkflowStateAccessRuleCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage()
    {
        return CreateFormFields.Page("Create workflow state access rule", Prefix+"/WorkflowStateAccessRuleResponse/create-form", Prefix+"/WorkflowStateAccessRuleResponse/list",
            "Defines how source roles are compiled into RBAC roles for a request, submission or form-node scope.",
            CreateFormFields.EntityLookup("workflow_state_id", "Workflow state", Prefix+"/WorkflowStateResponse/list", ["name", "key"]),
            CreateFormFields.EntityLookup("source_role_type_id", "Source role type", "/architecture/RoleTypeResponse/list", ["name"]),
            CreateFormFields.EntityLookup("target_role_type_id", "Target role type", "/architecture/RoleTypeResponse/list", ["name"]),
            CreateFormFields.Text("target_scope", "Target scope", help:"request, submission, or form-node"),
            CreateFormFields.EntityLookup("target_form_node_id", "Target form node", Prefix+"/FormNodeResponse/list", ["key", "label"], false));
    }
}
