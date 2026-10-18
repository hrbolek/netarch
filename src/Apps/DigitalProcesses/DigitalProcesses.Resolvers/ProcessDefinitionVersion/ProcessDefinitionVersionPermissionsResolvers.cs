using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WebAppArchitecture.DigitalProcesses.Services.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.UI.ProcessDefinitionVersion.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.ProcessDefinitionVersion;



public static class ProcessDefinitionVersionPermissionsResolvers
{
    public static async Task<IResult> ApplicantMatrix(Guid id, IProcessDefinitionVersionService versions, CancellationToken ct)
    {
        var page = await versions.ApplicantMatrixAsync(id, ct);
        if (page is null) return Results.NotFound();
        return new RazorComponentResult<ProcessPermissionsPage>(new Dictionary<string, object?>
        {
            ["Model"] = page.Model,
            ["MayEdit"] = page.MayEdit,
            ["States"] = page.States,
            ["Nodes"] = page.Nodes
        });
    }
}
