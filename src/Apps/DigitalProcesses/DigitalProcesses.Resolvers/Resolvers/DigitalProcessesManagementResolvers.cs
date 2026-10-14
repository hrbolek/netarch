using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.UI.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class DigitalProcessesManagementResolvers
{
    public static async Task<IResult> ApplicantMatrix(Guid id, IDigitalProcessesManagementService management, CancellationToken ct)
    {
        var page = await management.ApplicantMatrixAsync(id, ct);
        if (page is null) return Results.NotFound();
        return new RazorComponentResult<ProcessPermissionsPage>(new Dictionary<string, object?>
        {
            ["Model"] = page.Model,
            ["MayEdit"] = page.MayEdit,
            ["States"] = page.States,
            ["Nodes"] = page.Nodes
        });
    }

    public static async Task<IResult> TemplatePage(Guid id, IDigitalProcessesManagementService management, CancellationToken ct)
    {
        var page = await management.TemplatePageAsync(id, ct);
        if (page is null) return Results.NotFound();
        return new RazorComponentResult<FormTemplatesPage>(new Dictionary<string, object?>
        {
            ["Model"] = page.Model,
            ["MayEdit"] = page.MayEdit
        });
    }
}
