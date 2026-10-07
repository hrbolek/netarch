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

public static class WorkflowStateResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IWorkflowStateService service, CancellationToken ct) => BaseResolvers.Read<WorkflowStateModel,WorkflowStateResponse,IWorkflowStateService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IWorkflowStateService service, CancellationToken ct) => BaseResolvers.List<WorkflowStateModel,WorkflowStateResponse,IWorkflowStateService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IWorkflowStateService service, CancellationToken ct) => BaseResolvers.Link<WorkflowStateModel,WorkflowStateResponse,IWorkflowStateService>(id,http,service,ct);

    public static RazorComponentResult<DigitalProcessCreatePage> CreatePage() => DigitalProcessCreateForm.Page("Create workflow state", Prefix+"/WorkflowStateResponse/create-form", Prefix+"/WorkflowStateResponse/list", "Adds a state to a draft workflow version.", DigitalProcessCreateForm.GuidField("workflow_version_id","Workflow version ID"),DigitalProcessCreateForm.Text("key","Key"),DigitalProcessCreateForm.Text("name","Name"),DigitalProcessCreateForm.TextArea("description","Description",false),DigitalProcessCreateForm.Check("is_initial","Initial state"),DigitalProcessCreateForm.Check("is_terminal","Terminal state"),DigitalProcessCreateForm.Number("position","Position","0"));

    public static Task<WorkflowStateResponse> CreateApi(CreateWorkflowStateRequest request, IWorkflowStateService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IWorkflowStateService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateWorkflowStateRequest(DigitalProcessCreateForm.GuidValue(f,"workflow_version_id"),DigitalProcessCreateForm.Required(f,"key"),DigitalProcessCreateForm.Required(f,"name"),DigitalProcessCreateForm.Optional(f,"description"),DigitalProcessCreateForm.BoolValue(f,"is_initial"),DigitalProcessCreateForm.BoolValue(f,"is_terminal"),DigitalProcessCreateForm.IntValue(f,"position")), ct);
        return Results.Redirect(Prefix + "/WorkflowStateResponse/view/" + created.id);
    }

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowStateResponse").WithTags("DigitalProcesses.Workflow");
        group.MapGet("/view/{id:guid}", WorkflowStateResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<WorkflowStateResponse>,WorkflowStateResponse>()
            .WithDescription("""
                Returns one WorkflowState visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", WorkflowStateResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<WorkflowStateResponse>,WorkflowStateResponse[]>()
            .WithDescription("""
                Returns WorkflowState entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", WorkflowStateResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-contained", WorkflowStateResolvers.CreateApi)
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowStateResponse>,WorkflowStateResponse>()
            .WithDescription("""
                Creates WorkflowState using parent WorkflowVersion. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", WorkflowStateResolvers.CreatePage)
            .WithDescription("""
                Renders the HTML form used to create WorkflowState.
                """);
        group.MapPost("/create-form", WorkflowStateResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates WorkflowState from an HTML form submission and redirects to its detail page.
                """);
    }
}
