using WebAppArchitecture.DigitalProcesses.Models.Shared;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNodeType;
/// <summary>Implemented component metadata shared by the catalog and the AI designer.</summary>
public static class FormComponentCatalog
{
    private sealed record Definition(string Description, object ConfigurationExample);
    private static readonly IReadOnlyDictionary<string, Definition> Examples = new Dictionary<string, Definition>(StringComparer.Ordinal)
    {
        ["section"] = new("Container for child nodes", new { }),
        ["input.text"] = new("Single line input", new { placeholder = "Name" }),
        ["input.textarea"] = new("Multi-line input", new { placeholder = "Comment" }),
        ["input.number"] = new("Number input", new { }),
        ["input.date"] = new("Date input", new { }),
        ["input.time"] = new("Time input", new { }),
        ["input.checkbox"] = new("Boolean input", new { }),
        ["input.select"] = new("Choice input; JSONata results use data.items with label and value", new { dataSource = new { query = "query { items { label value } }", variables = "section" } }),
        ["input.graphql-search"] = new("Searches GraphQL after three characters; the input is sent as the 'fragment' variable. Configure the query to return data.choices and alias its selected fields as 'id' and 'label'. These response names are fixed and are not configurable.", new
        {
            query = "query userread($fragment: Str) { choices: userList(where: { email: { _ilike: $fragment } }) { id label: email } }",
            minLength = 3,
            variables = new { }
        }),
        ["display.url"] = new("Read-only HTTP(S) link rendered locally from FormDataContext; no backend query", new { template = "https://data.company.com/users/{$.form.sekce_a.user_id}", label = "Otevřít profil", target = "_blank" }),
        ["action.button"] = new("Action button", new { label = "Akce" }),
        ["action.import"] = new("Import named JSONata result fields to form", new { label = "Importovat", query = "query { whoami { hostname } }", variables = new { } }),
        ["action.export"] = new("Export form data to configured backend", new { label = "Exportovat", query = "query { whoami { hostname } }", variables = new { } }),
        ["action.document"] = new("Generate RTF document from configured template", new { label = "Dokument", templateId = "00000000-0000-0000-0000-000000000000" })
    };

    public static bool IsSupported(string rendererKey) => Examples.ContainsKey(rendererKey);
    public static string GetDescription(string rendererKey) => Examples[rendererKey].Description;
    public static object GetExample(string rendererKey) => Examples[rendererKey].ConfigurationExample;
}

public sealed class FormComponentCatalogService(IFormNodeTypeService nodeTypes) : IFormComponentCatalogService
{
    public AuxiliaryOperationResult GetCatalog()
    {
        var types = nodeTypes.GetRendererTypes();
        var components = types.Where(x => FormComponentCatalog.IsSupported(x.renderer_key)).Select(x => new
        {
            x.id, x.renderer_key, x.name,
            description = FormComponentCatalog.GetDescription(x.renderer_key),
            x.can_have_children, x.can_have_value, x.can_repeat,
            configuration_example = FormComponentCatalog.GetExample(x.renderer_key)
        }).ToArray();
        return AuxiliaryOperationResult.Ok(new { schema_version = "1.0", components });
    }
}
