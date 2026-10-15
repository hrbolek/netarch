using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.FormVersion.Components;

public static class FormVersionCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page("Create form version", Prefix+"/FormVersionResponse/create-form", Prefix+"/FormVersionResponse/list", "Creates a draft version of an existing form definition.", CreateFormFields.GuidField("form_definition_id","Form definition ID"), CreateFormFields.Number("version","Version","1","1"), CreateFormFields.Check("publish","Publish immediately"));
}
