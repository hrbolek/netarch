using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.UI.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class WorkflowVersionResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IWorkflowVersionService service, CancellationToken ct) => BaseResolvers.Read<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IWorkflowVersionService service, CancellationToken ct) => BaseResolvers.List<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IWorkflowVersionService service, CancellationToken ct) => BaseResolvers.Link<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService>(id,http,service,ct);

    public static RazorComponentResult<DigitalProcessCreatePage> CreatePage() => DigitalProcessCreateForm.Page("Create workflow version", Prefix+"/WorkflowVersionResponse/create-form", Prefix+"/WorkflowVersionResponse/list", "Creates a draft version of a workflow definition.", DigitalProcessCreateForm.GuidField("workflow_definition_id","Workflow definition ID"),DigitalProcessCreateForm.Number("version","Version","1","1"),DigitalProcessCreateForm.Check("publish","Publish immediately"));

    public static Task<WorkflowVersionResponse> CreateApi(CreateWorkflowVersionRequest request, IWorkflowVersionService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IWorkflowVersionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateWorkflowVersionRequest(DigitalProcessCreateForm.GuidValue(f,"workflow_definition_id"),DigitalProcessCreateForm.IntValue(f,"version"),DigitalProcessCreateForm.BoolValue(f,"publish")), ct);
        return Results.Redirect(Prefix + "/WorkflowVersionResponse/view/" + created.id);
    }
    public static Task<WorkflowVersionResponse> Publish(Guid id, IWorkflowVersionService service, CancellationToken ct) => service.PublishAsync(id, ct);

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowVersionResponse").WithTags("DigitalProcesses.Workflow");
        group.MapGet("/view/{id:guid}", WorkflowVersionResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<WorkflowVersionResponse>,WorkflowVersionResponse>()
            .WithDescription("""
                Returns one WorkflowVersion visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", WorkflowVersionResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<WorkflowVersionResponse>,WorkflowVersionResponse[]>()
            .WithDescription("""
                Returns WorkflowVersion entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", WorkflowVersionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-contained", WorkflowVersionResolvers.CreateApi)
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowVersionResponse>,WorkflowVersionResponse>()
            .WithDescription("""
                Creates WorkflowVersion using parent WorkflowDefinition. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", WorkflowVersionResolvers.CreatePage)
            .WithDescription("""
                Renders the HTML form used to create WorkflowVersion.
                """);
        group.MapPost("/create-form", WorkflowVersionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates WorkflowVersion from an HTML form submission and redirects to its detail page.
                """);
        group.MapPost("/publish/{id:guid}", WorkflowVersionResolvers.Publish)
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowVersionResponse>,WorkflowVersionResponse>()
            .WithDescription("""
                Publishes a workflow version and freezes its state graph.
                """);
    }
}
