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
using WebAppArchitecture.DigitalProcesses.UI.Designer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

public static class WorkflowVersionResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IWorkflowVersionService service, CancellationToken ct) => BaseResolvers.Read<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IWorkflowVersionService service, CancellationToken ct) => BaseResolvers.List<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IWorkflowVersionService service, CancellationToken ct) => BaseResolvers.Link<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService>(id,http,service,ct);

    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page("Create workflow version", Prefix+"/WorkflowVersionResponse/create-form", Prefix+"/WorkflowVersionResponse/list", "Creates a draft version of a workflow definition.", DigitalProcessCreateForm.GuidField("workflow_definition_id","Workflow definition ID"),DigitalProcessCreateForm.Number("version","Version","1","1"),DigitalProcessCreateForm.Check("publish","Publish immediately"));

    public static Task<WorkflowVersionResponse> CreateApi(CreateWorkflowVersionRequest request, IWorkflowVersionService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IWorkflowVersionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateWorkflowVersionRequest(DigitalProcessCreateForm.GuidValue(f,"workflow_definition_id"),DigitalProcessCreateForm.IntValue(f,"version"),DigitalProcessCreateForm.BoolValue(f,"publish")), ct);
        return Results.Redirect(Prefix + "/WorkflowVersionResponse/designer/" + created.id);
    }
    public static Task<WorkflowVersionResponse> Publish(Guid id, IWorkflowVersionService service, CancellationToken ct) => service.PublishAsync(id, ct);

    public static Task<WorkflowDesignerResponse> Designer(Guid id, IWorkflowVersionService service, CancellationToken ct)
        => service.GetDesignerAsync(id, ct);

    private static string? Optional(Microsoft.AspNetCore.Http.IFormCollection f, string name)
        => DigitalProcessCreateForm.Optional(f, name);
    private static bool Checked(Microsoft.AspNetCore.Http.IFormCollection f, string name)
        => DigitalProcessCreateForm.BoolValue(f, name);
    private static string Url(Guid id) => $"{Prefix}/WorkflowVersionResponse/designer/{id}";

    public static async Task<IResult> AddState(HttpContext http, IWorkflowStateService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = DigitalProcessCreateForm.GuidValue(f, "workflow_version_id");
        await service.CreateContainedAsync(new CreateWorkflowStateRequest(versionId,
            DigitalProcessCreateForm.Required(f, "key"), DigitalProcessCreateForm.Required(f, "name"),
            Optional(f, "description"), Checked(f, "is_initial"), Checked(f, "is_terminal"),
            DigitalProcessCreateForm.IntValue(f, "position")), ct);
        return Results.Redirect(Url(versionId));
    }

    public static async Task<IResult> UpdateState(HttpContext http, IWorkflowStateService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = DigitalProcessCreateForm.GuidValue(f, "workflow_version_id");
        await service.UpdateDefinitionAsync(DigitalProcessCreateForm.GuidValue(f, "id"),
            DigitalProcessCreateForm.Required(f, "key"), DigitalProcessCreateForm.Required(f, "name"),
            Optional(f, "description"), Checked(f, "is_initial"), Checked(f, "is_terminal"),
            DigitalProcessCreateForm.IntValue(f, "position"), ct);
        return Results.Redirect(Url(versionId));
    }

    public static async Task<IResult> DeleteState(HttpContext http, IWorkflowStateService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        await service.DeleteDefinitionAsync(DigitalProcessCreateForm.GuidValue(f, "id"), ct);
        return Results.Redirect(Url(DigitalProcessCreateForm.GuidValue(f, "workflow_version_id")));
    }

    public static async Task<IResult> AddTransition(HttpContext http, IWorkflowTransitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = DigitalProcessCreateForm.GuidValue(f, "workflow_version_id");
        await service.CreateContainedAsync(new CreateWorkflowTransitionRequest(versionId,
            DigitalProcessCreateForm.GuidValue(f, "source_state_id"),
            DigitalProcessCreateForm.GuidValue(f, "target_state_id"),
            DigitalProcessCreateForm.Required(f, "key"), DigitalProcessCreateForm.Required(f, "name"),
            Checked(f, "note_required"), Optional(f, "condition_query"), Optional(f, "action_query")), ct);
        return Results.Redirect(Url(versionId));
    }

    public static async Task<IResult> UpdateTransition(HttpContext http, IWorkflowTransitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = DigitalProcessCreateForm.GuidValue(f, "workflow_version_id");
        await service.UpdateDefinitionAsync(DigitalProcessCreateForm.GuidValue(f, "id"),
            DigitalProcessCreateForm.GuidValue(f, "source_state_id"),
            DigitalProcessCreateForm.GuidValue(f, "target_state_id"),
            DigitalProcessCreateForm.Required(f, "key"), DigitalProcessCreateForm.Required(f, "name"),
            Checked(f, "note_required"), Optional(f, "condition_query"), Optional(f, "action_query"), ct);
        return Results.Redirect(Url(versionId));
    }

    public static async Task<IResult> DeleteTransition(HttpContext http, IWorkflowTransitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        await service.DeleteDefinitionAsync(DigitalProcessCreateForm.GuidValue(f, "id"), ct);
        return Results.Redirect(Url(DigitalProcessCreateForm.GuidValue(f, "workflow_version_id")));
    }

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowVersionResponse").WithTags("DigitalProcesses.Workflow");
        group.MapActions<WorkflowVersionModel>("WorkflowVersionResponse");
        group.MapGet("/designer/{id:guid}", WorkflowVersionResolvers.Designer)
            .ApplyViewModel<WorkflowDesignerPage, WorkflowDesignerResponse>()
            .WithDescription("Opens the workflow graph editor with its states and transitions. Published versions are read-only.");
        group.MapPost("/designer/add-state", WorkflowVersionResolvers.AddState).DisableAntiforgery()
            .WithDescription("Adds a workflow state to an unpublished workflow version.");
        group.MapPost("/designer/update-state", WorkflowVersionResolvers.UpdateState).DisableAntiforgery()
            .WithDescription("Updates state properties and initial/terminal flags in a draft workflow.");
        group.MapPost("/designer/delete-state", WorkflowVersionResolvers.DeleteState).DisableAntiforgery()
            .WithDescription("Deletes a draft workflow state when no transitions reference it.");
        group.MapPost("/designer/add-transition", WorkflowVersionResolvers.AddTransition).DisableAntiforgery()
            .WithDescription("Adds a transition between states belonging to the current draft workflow version.");
        group.MapPost("/designer/update-transition", WorkflowVersionResolvers.UpdateTransition).DisableAntiforgery()
            .WithDescription("Updates transition endpoints, label and condition/action queries.");
        group.MapPost("/designer/delete-transition", WorkflowVersionResolvers.DeleteTransition).DisableAntiforgery()
            .WithDescription("Deletes a transition from an unpublished workflow version.");
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
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
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
