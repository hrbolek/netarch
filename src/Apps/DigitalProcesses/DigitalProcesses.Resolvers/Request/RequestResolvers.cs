using WebAppArchitecture.DigitalProcesses.UI.Request.Components;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.Request;
using WebAppArchitecture.DigitalProcesses.UI.Request.Pages;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
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

namespace WebAppArchitecture.DigitalProcesses.Resolvers.Request;
/// <summary>
/// Vstupní kontrakt Request Resolvers. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public static class RequestResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, StartAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IRequestService service, CancellationToken ct) => BaseResolvers.Read<RequestModel,RequestResponse,IRequestService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, StartAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IRequestService service, CancellationToken ct) => BaseResolvers.List<RequestModel,RequestResponse,IRequestService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, StartAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IRequestService service, CancellationToken ct) => BaseResolvers.Link<RequestModel,RequestResponse,IRequestService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: StartAsync, TransitionAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => RequestCreateForm.CreatePage();

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: StartAsync, TransitionAsync, GetDetailAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IRequestService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.StartAsync(new StartRequestRequest(
            OptionalGuid(f, "process_definition_version_id") ?? Guid.Empty,
            CreateFormValues.Optional(f, "subject"),
            OptionalGuid(f, "target_group_id"),
            OptionalGuid(f, "process_definition_id")), ct);
        return Results.Redirect(Prefix + "/RequestResponse/detail/" + created.id);
    }
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: StartAsync, TransitionAsync, GetDetailAsync; pořadí a parametry viz implementace.
    /// </summary>
    private static Guid? OptionalGuid(IFormCollection form, string name)
        => Guid.TryParse(form[name].ToString(), out var value) && value != Guid.Empty ? value : null;

    public static Task<RequestResponse> CreateTest(Guid processId, IRequestService service, CancellationToken ct) => service.CreateTestRequestAsync(processId, ct);
    public static Task<RequestResponse> Start(StartRequestRequest request, IRequestService service, CancellationToken ct) => service.StartAsync(request, ct);
    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: TransitionAsync, GetDetailAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<RequestResponse> Transition(Guid id, TransitionRequestRequest request, IRequestService service, CancellationToken ct) => service.TransitionAsync(id, request, ct);
    public static async Task<IResult> TransitionForm(Guid id, HttpContext http, IRequestService service, CancellationToken ct)
    {
        var form = await http.Request.ReadFormAsync(ct);
        var request = await service.GetByIdAsync<RequestResponse>(id, ct)
            ?? throw new KeyNotFoundException("Request not found or not readable.");
        var transitionId = Guid.TryParse(form["transition_id"], out var parsed)
            ? parsed
            : throw new BadHttpRequestException("transition_id must be a GUID.");
        await service.TransitionAsync(id, new TransitionRequestRequest(transitionId, CreateFormValues.Optional(form, "note")), ct);
        return Results.Redirect($"{Prefix}/ProcessDefinitionVersionResponse/test/{request.process_definition_version_id}?requestId={id}");
    }
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: GetDetailAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<RequestDetailResponse> Detail(Guid id, IRequestService service, CancellationToken ct) => service.GetDetailAsync(id, ct);

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/RequestResponse").WithTags("DigitalProcesses.Runtime");
        group.MapActions<RequestModel>("RequestResponse");
        group.MapGet("/view/{id:guid}", RequestResolvers.Read)
            .ApplyViewModel<RequestModelPage,RequestResponse>()
            .WithDescription("""
                Returns one Request visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", RequestResolvers.List)
            .ApplyViewModel<RequestListPage,RequestResponse[]>()
            .WithDescription("""
                Returns Request entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", RequestResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapGet("/create", RequestResolvers.CreatePage)
            .ApplyViewModel<RequestCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create Request.
                """);
        group.MapPost("/create-form", RequestResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates Request from an HTML form submission and redirects to its detail page.
                """);
        group.MapPost("/start", RequestResolvers.Start)
            .ApplyJsonModel<RequestModelPage,RequestResponse>()
            .WithDescription("""
                Starts a request, records the initiator and creates the first submission and RBAC scopes.
                """);
        group.MapPost("/test/{processId:guid}", RequestResolvers.CreateTest)
            .ApplyJsonModel<RequestModelPage,RequestResponse>()
            .WithDescription("""
                Creates a demo Request with transition history on the latest version of the process, including an unpublished one.
                """);
        group.MapPost("/transition/{id:guid}", RequestResolvers.Transition)
            .ApplyJsonModel<RequestModelPage,RequestResponse>()
            .WithDescription("""
                Archives the current submission, creates a new RBAC-secured submission snapshot and moves the request to the target workflow state.
                """);
        group.MapPost("/transition-form/{id:guid}", RequestResolvers.TransitionForm)
            .DisableAntiforgery()
            .WithDescription("Executes an authorized workflow transition from the process test screen and redirects back to that exact test.");
        group.MapGet("/detail/{id:guid}", RequestResolvers.Detail)
            .ApplyViewModel<RequestDetailPage,RequestDetailResponse>()
            .WithDescription("""
                Returns a request with current workflow state, active submission and transition history.
                """);
    }
}
