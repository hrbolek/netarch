using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;
using WebAppArchitecture.DigitalProcesses.UI.FormVersion.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormVersion;

public static class FormVersionTemplatesResolvers
{
    public static IResult LegacyTemplatePage(Guid id)
        => Results.Redirect($"/digital-processes/FormVersionResponse/templates/{id}");

    public static async Task<IResult> TemplatePage(Guid id, IFormVersionService versions, CancellationToken ct)
    {
        var page = await versions.TemplatePageAsync(id, ct);
        if (page is null) return Results.NotFound();
        return new RazorComponentResult<FormTemplatesPage>(new Dictionary<string, object?>
        {
            ["Model"] = page.Model,
            ["MayEdit"] = page.MayEdit
        });
    }
}
