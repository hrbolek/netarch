using WebAppArchitecture.DigitalProcesses.UI.WorkflowTransition.Components;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.UI.WorkflowTransition.Pages;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.WorkflowTransition;
/// <summary>
/// HTTP resolver pro Workflow Transition Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>

public static class WorkflowTransitionResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IWorkflowTransitionService service, CancellationToken ct) => BaseResolvers.Read<WorkflowTransitionModel, WorkflowTransitionResponse, IWorkflowTransitionService>(id, http, service, ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IWorkflowTransitionService service, CancellationToken ct) => BaseResolvers.List<WorkflowTransitionModel, WorkflowTransitionResponse, IWorkflowTransitionService>(http, service, ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IWorkflowTransitionService service, CancellationToken ct) => BaseResolvers.Link<WorkflowTransitionModel, WorkflowTransitionResponse, IWorkflowTransitionService>(id, http, service, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => WorkflowTransitionCreateForm.CreatePage();

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<WorkflowTransitionResponse> CreateApi(CreateWorkflowTransitionRequest request, IWorkflowTransitionService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IWorkflowTransitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateWorkflowTransitionRequest(CreateFormValues.GuidValue(f, "workflow_version_id"), CreateFormValues.GuidValue(f, "source_state_id"), CreateFormValues.GuidValue(f, "target_state_id"), CreateFormValues.Required(f, "key"), CreateFormValues.Required(f, "name"), CreateFormValues.BoolValue(f, "note_required"), CreateFormValues.Optional(f, "condition_query"), CreateFormValues.Optional(f, "action_query")), ct);
        return Results.Redirect(Prefix + "/WorkflowTransitionResponse/view/" + created.id);
    }

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowTransitionResponse").WithTags("DigitalProcesses.Workflow");
        group.MapActions<WorkflowTransitionModel>("WorkflowTransitionResponse");
        group.MapGet("/view/{id:guid}", WorkflowTransitionResolvers.Read)
            .ApplyViewModel<WorkflowTransitionModelPage, WorkflowTransitionResponse>()
            .WithDescription("""
                Returns one WorkflowTransition visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", WorkflowTransitionResolvers.List)
            .ApplyViewModel<WorkflowTransitionListPage, WorkflowTransitionResponse[]>()
            .WithDescription("""
                Returns WorkflowTransition entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", WorkflowTransitionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-contained", WorkflowTransitionResolvers.CreateApi)
            .ApplyJsonModel<WorkflowTransitionModelPage, WorkflowTransitionResponse>()
            .WithDescription("""
                Creates WorkflowTransition using parent WorkflowVersion. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", WorkflowTransitionResolvers.CreatePage)
            .ApplyViewModel<WorkflowTransitionCreatePage, DigitalProcessCreatePageModel>()
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
