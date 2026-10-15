using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.FormDefinition.Components;

public static class FormDefinitionCreateForm
{
    private const string Prefix = "/digital-processes";

    public static DigitalProcessCreatePageModel CreatePage() => CreateFormFields.Page("Create form definition", Prefix+"/FormDefinitionResponse/create-form", Prefix+"/FormDefinitionResponse/list", "Creates the stable identity of a form together with draft version 1. Form structure is edited in that draft version.", CreateFormFields.Text("name","Name"), CreateFormFields.TextArea("description","Description",false));
}
