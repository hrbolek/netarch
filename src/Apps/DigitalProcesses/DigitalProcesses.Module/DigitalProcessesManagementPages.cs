using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.UI.Pages;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>
/// HTML presentation is implemented exclusively by Razor components.
/// Endpoint handlers load authorized data and supply immutable view models.
/// </summary>
public static class DigitalProcessesManagementPages
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/digital-processes/process-versions/{id:guid}/permissions", ApplicantMatrix)
            .WithTags("DigitalProcesses.Designer")
            .WithDescription("Razor-based Applicant access matrix. Draft process versions only allow editing when the current user has Update.");
        endpoints.MapGet("/digital-processes/form-versions/{id:guid}/templates", TemplatePage)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("Razor-based RTF template management for a FormVersion.");
    }

    private static async Task<IResult> ApplicantMatrix(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        var process = await security.GetByIdAsync<ProcessDefinitionVersionModel>(id, SecurityOperations.Read, ct);
        if (process is null) return Results.NotFound();
        var mayEdit = !process.is_published &&
            await security.GetByIdAsync<ProcessDefinitionVersionModel>(id, SecurityOperations.Update, ct) is not null;
        var states = security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Where(x => x.workflow_version_id == process.workflow_version_id)
            .OrderBy(x => x.position)
            .ToArray().Select(WorkflowStateResponse.FromModel).ToArray();
        // Preserve sibling order instead of sorting path strings lexically.
        var nodes = security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == process.form_version_id)
            .ToArray().Select(FormNodeResponse.FromModel).ToArray();
        var parameters = new Dictionary<string, object?>
        {
            ["Model"] = ProcessDefinitionVersionResponse.FromModel(process),
            ["MayEdit"] = mayEdit,
            ["States"] = states,
            ["Nodes"] = nodes
        };
        return new Microsoft.AspNetCore.Http.HttpResults.RazorComponentResult<ProcessPermissionsPage>(parameters);
    }

    private static async Task<IResult> TemplatePage(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        var version = await security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Read, ct);
        if (version is null) return Results.NotFound();
        var mayEdit = !version.is_published &&
            await security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Update, ct) is not null;
        return new Microsoft.AspNetCore.Http.HttpResults.RazorComponentResult<FormTemplatesPage>(
            new Dictionary<string, object?>
            {
                ["Model"] = FormVersionResponse.FromModel(version),
                ["MayEdit"] = mayEdit
            });
    }
}
