using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>Agent-facing inventory of installed, readable form elements.
/// Configuration examples are illustrative complete objects, not JSON Schema.</summary>
public static class FormComponentCatalogEndpoints
{
    private sealed record Definition(string Description, object ConfigurationExample);
    private static readonly IReadOnlyDictionary<string, Definition> Examples = new Dictionary<string, Definition>(StringComparer.Ordinal)
    {
        ["section"] = new("Container for child nodes", new { }),
        ["input.text"] = new("Single line input", new { placeholder = "Name" }),
        ["input.textarea"] = new("Multi-line input", new { placeholder = "Comment" }),
        ["input.number"] = new("Number input", new { }),
        ["input.date"] = new("Date input", new { }),
        ["input.checkbox"] = new("Boolean input", new { }),
        ["input.select"] = new("Choice input; JSONata results use data.items with label and value", new { dataSource = new { query = "query { items { label value } }", variables = "section" } }),
        ["display.url"] = new("Read-only HTTP(S) link rendered locally from FormDataContext; no backend query", new { template = "https://data.company.com/users/{$.form.sekce_a.user_id}", label = "Otevřít profil", target = "_blank" }),
        ["action.button"] = new("Action button", new { label = "Akce" }),
        ["action.import"] = new("Import named JSONata result fields to form", new { label = "Importovat", query = "query { whoami { hostname } }", variables = new { } }),
        ["action.export"] = new("Export form data to configured backend", new { label = "Exportovat", query = "query { whoami { hostname } }", variables = new { } }),
        ["action.document"] = new("Generate RTF document from configured template", new { label = "Dokument", templateId = "00000000-0000-0000-0000-000000000000" })
    };

    public static bool IsSupported(string rendererKey) => Examples.ContainsKey(rendererKey);
    public static string GetDescription(string rendererKey) => Examples[rendererKey].Description;
    public static object GetExample(string rendererKey) => Examples[rendererKey].ConfigurationExample;

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/digital-processes/form-component-catalog", (ISecurityLayer security) =>
        {
            var types = security.Query<FormNodeTypeModel>(SecurityOperations.Read)
                .OrderBy(x => x.renderer_key).ToArray();
            var components = types.Where(x => Examples.ContainsKey(x.renderer_key)).Select(x => new
            {
                x.id, x.renderer_key, x.name,
                description = Examples[x.renderer_key].Description,
                x.can_have_children, x.can_have_value, x.can_repeat,
                configuration_example = Examples[x.renderer_key].ConfigurationExample
            }).ToArray();
            return Results.Ok(new { schema_version = "1.0", components });
        }).WithTags("DigitalProcesses.AI")
          .WithDescription("Returns readable, implemented form node types and complete example configurations for schema-generating agents. RBAC-filtered.");
    }
}
