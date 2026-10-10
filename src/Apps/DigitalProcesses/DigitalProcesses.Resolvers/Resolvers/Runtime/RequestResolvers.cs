using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Base.Security;
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

public static class RequestResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IRequestService service, CancellationToken ct) => BaseResolvers.Read<RequestModel,RequestResponse,IRequestService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IRequestService service, CancellationToken ct) => BaseResolvers.List<RequestModel,RequestResponse,IRequestService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IRequestService service, CancellationToken ct) => BaseResolvers.Link<RequestModel,RequestResponse,IRequestService>(id,http,service,ct);

    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page(
        "Create request", Prefix + "/RequestResponse/create-form", Prefix + "/RequestResponse/list",
        "Select a process and its published version. The acting user becomes the initiator.",
        DigitalProcessCreateForm.EntityLookup("process_definition_id", "Process", Prefix + "/ProcessDefinitionResponse/list", ["name"]),
        DigitalProcessCreateForm.EntityLookup("process_definition_version_id", "Published process version", Prefix + "/ProcessDefinitionVersionResponse/list?where=%7B%22is_published%22%3A%7B%22_eq%22%3Atrue%7D%7D", [], parentField: "process_definition_id", parentProperty: "process_definition_id"),
        DigitalProcessCreateForm.EntityLookup("target_group_id", "Target organisation", "/architecture/GroupResponse/list", ["Name"]),
        DigitalProcessCreateForm.Text("subject", "Subject", false));

    public static async Task<IResult> CreateForm(HttpContext http, IRequestService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.StartAsync(new StartRequestRequest(DigitalProcessCreateForm.GuidValue(f,"process_definition_version_id"),DigitalProcessCreateForm.Optional(f,"subject"),DigitalProcessCreateForm.GuidValue(f,"target_group_id")), ct);
        return Results.Redirect(Prefix + "/RequestResponse/detail/" + created.id);
    }
    public static Task<RequestResponse> Start(StartRequestRequest request, IRequestService service, CancellationToken ct) => service.StartAsync(request, ct);
    public static Task<RequestResponse> Transition(Guid id, TransitionRequestRequest request, IRequestService service, CancellationToken ct) => service.TransitionAsync(id, request, ct);
    public static Task<RequestDetailResponse> Detail(Guid id, IRequestService service, CancellationToken ct) => service.GetDetailAsync(id, ct);

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/RequestResponse").WithTags("DigitalProcesses.Runtime");
        group.MapActions<RequestModel>("RequestResponse");
        group.MapGet("/view/{id:guid}", RequestResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<RequestResponse>,RequestResponse>()
            .WithDescription("""
                Returns one Request visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", RequestResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<RequestResponse>,RequestResponse[]>()
            .WithDescription("""
                Returns Request entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", RequestResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapGet("/create", RequestResolvers.CreatePage)
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create Request.
                """);
        group.MapPost("/create-form", RequestResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates Request from an HTML form submission and redirects to its detail page.
                """);
        group.MapPost("/start", RequestResolvers.Start)
            .ApplyJsonModel<DigitalProcessModelPage<RequestResponse>,RequestResponse>()
            .WithDescription("""
                Starts a request, records the initiator and creates the first submission and RBAC scopes.
                """);
        group.MapPost("/transition/{id:guid}", RequestResolvers.Transition)
            .ApplyJsonModel<DigitalProcessModelPage<RequestResponse>,RequestResponse>()
            .WithDescription("""
                Archives the current submission, creates a new RBAC-secured submission snapshot and moves the request to the target workflow state.
                """);
        group.MapGet("/detail/{id:guid}", RequestResolvers.Detail)
            .ApplyViewModel<RequestDetailPage,RequestDetailResponse>()
            .WithDescription("""
                Returns a request with current workflow state, active submission and transition history.
                """);
    }
}
