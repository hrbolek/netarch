using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.FormNode.Components;

public static class FormNodeCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page("Create form node", Prefix+"/FormNodeResponse/create-form", Prefix+"/FormNodeResponse/list", "Adds a node to a draft form-version tree. The materialized path is computed by the service.", CreateFormFields.GuidField("form_version_id","Form version ID"), CreateFormFields.Text("key","Key"), CreateFormFields.GuidField("node_type_id","Node type ID"), CreateFormFields.GuidField("parent_id","Parent node ID",false), CreateFormFields.Text("label","Label",false), CreateFormFields.TextArea("description","Description",false), CreateFormFields.Number("position","Position","0"), CreateFormFields.Number("min_occurs","Min occurs","1","0"), CreateFormFields.Number("max_occurs","Max occurs","1","1"), CreateFormFields.Check("own_security_scope","Own security scope"), CreateFormFields.TextArea("configuration_json","Configuration JSON",false));
}
