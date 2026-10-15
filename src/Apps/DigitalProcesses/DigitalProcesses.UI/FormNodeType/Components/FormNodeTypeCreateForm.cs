using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.FormNodeType.Components;

public static class FormNodeTypeCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page("Create form node type", Prefix+"/FormNodeTypeResponse/create-form", Prefix+"/FormNodeTypeResponse/list", "Defines form-node capabilities and the renderer key used by the Razor renderer registry.", CreateFormFields.Text("name","Name"), CreateFormFields.Text("renderer_key","Renderer key",help:"Examples: section, input.text, input.number, input.date, input.checkbox, action.button"), CreateFormFields.Check("can_have_children","Can have children"), CreateFormFields.Check("can_have_value","Can have value"), CreateFormFields.Check("can_repeat","Can repeat"), CreateFormFields.GuidField("parent_type_id","Parent type ID",false), CreateFormFields.TextArea("configuration_schema_json","Configuration schema JSON",false));
}
