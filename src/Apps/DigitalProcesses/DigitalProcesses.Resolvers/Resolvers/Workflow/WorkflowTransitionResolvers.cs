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

public static class WorkflowTransitionResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IWorkflowTransitionService service, CancellationToken ct) => BaseResolvers.Read<WorkflowTransitionModel,WorkflowTransitionResponse,IWorkflowTransitionService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IWorkflowTransitionService service, CancellationToken ct) => BaseResolvers.List<WorkflowTransitionModel,WorkflowTransitionResponse,IWorkflowTransitionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IWorkflowTransitionService service, CancellationToken ct) => BaseResolvers.Link<WorkflowTransitionModel,WorkflowTransitionResponse,IWorkflowTransitionService>(id,http,service,ct);

    public static RazorComponentResult<DigitalProcessCreatePage> CreatePage() => DigitalProcessCreateForm.Page("Create workflow transition", Prefix+"/WorkflowTransitionResponse/create-form", Prefix+"/WorkflowTransitionResponse/list", "Adds a directed transition between two states of a draft workflow version.", DigitalProcessCreateForm.GuidField("workflow_version_id","Workflow version ID"),DigitalProcessCreateForm.GuidField("source_state_id","Source state ID"),DigitalProcessCreateForm.GuidField("target_state_id","Target state ID"),DigitalProcessCreateForm.Text("key","Key"),DigitalProcessCreateForm.Text("name","Name"),DigitalProcessCreateForm.Check("note_required","Transition note required"),DigitalProcessCreateForm.TextArea("condition_query","Condition query",false),DigitalProcessCreateForm.TextArea("action_query","Action query",false));

    public static Task<WorkflowTransitionResponse> CreateApi(CreateWorkflowTransitionRequest request, IWorkflowTransitionService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IWorkflowTransitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateWorkflowTransitionRequest(DigitalProcessCreateForm.GuidValue(f,"workflow_version_id"),DigitalProcessCreateForm.GuidValue(f,"source_state_id"),DigitalProcessCreateForm.GuidValue(f,"target_state_id"),DigitalProcessCreateForm.Required(f,"key"),DigitalProcessCreateForm.Required(f,"name"),DigitalProcessCreateForm.BoolValue(f,"note_required"),DigitalProcessCreateForm.Optional(f,"condition_query"),DigitalProcessCreateForm.Optional(f,"action_query")), ct);
        return Results.Redirect(Prefix + "/WorkflowTransitionResponse/view/" + created.id);
    }

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowTransitionResponse").WithTags("DigitalProcesses.Workflow");
        group.MapGet("/view/{id:guid}", WorkflowTransitionResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<WorkflowTransitionResponse>,WorkflowTransitionResponse>()
            .WithDescription("""
                Returns one WorkflowTransition visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", WorkflowTransitionResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<WorkflowTransitionResponse>,WorkflowTransitionResponse[]>()
            .WithDescription("""
                Returns WorkflowTransition entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", WorkflowTransitionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-contained", WorkflowTransitionResolvers.CreateApi)
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowTransitionResponse>,WorkflowTransitionResponse>()
            .WithDescription("""
                Creates WorkflowTransition using parent WorkflowVersion. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", WorkflowTransitionResolvers.CreatePage)
            .WithDescription("""
                Renders the HTML form used to create WorkflowTransition.
                """);
        group.MapPost("/create-form", WorkflowTransitionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates WorkflowTransition from an HTML form submission and redirects to its detail page.
                """);
    }
}
