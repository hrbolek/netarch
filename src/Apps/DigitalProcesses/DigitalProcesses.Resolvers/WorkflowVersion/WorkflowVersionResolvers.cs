using WebAppArchitecture.DigitalProcesses.UI.WorkflowVersion.Components;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.Request;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using WebAppArchitecture.DigitalProcesses.UI.WorkflowVersion.Pages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.WorkflowVersion;
/// <summary>
/// HTTP resolver pro Workflow Version Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>

public static class WorkflowVersionResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, CreateContainedAsync, PublishAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IWorkflowVersionService service, CancellationToken ct) => BaseResolvers.Read<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateContainedAsync, PublishAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IWorkflowVersionService service, CancellationToken ct) => BaseResolvers.List<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateContainedAsync, PublishAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IWorkflowVersionService service, CancellationToken ct) => BaseResolvers.Link<WorkflowVersionModel,WorkflowVersionResponse,IWorkflowVersionService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, PublishAsync, GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => WorkflowVersionCreateForm.CreatePage();

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, PublishAsync, GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> CreateApi(CreateWorkflowVersionRequest request, IWorkflowVersionService service, CancellationToken ct)
        => DefinitionResult(async () => await service.CreateContainedAsync(request, ct));

    public static Task<IResult> UpdateApi(Guid id, WorkflowGraphDefinition request, IWorkflowVersionService service, CancellationToken ct)
        => DefinitionResult(async () => await service.ReplaceDefinitionAsync(id, request, ct));

    private static async Task<IResult> DefinitionResult(Func<Task<object>> action)
    {
        try { return Results.Ok(await action()); }
        catch (ArgumentException ex) { return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest); }
        catch (KeyNotFoundException ex) { return Results.Problem(ex.Message, statusCode: StatusCodes.Status404NotFound); }
        catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden); }
        catch (InvalidOperationException ex) { return Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict); }
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, PublishAsync, GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IWorkflowVersionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateWorkflowVersionRequest(CreateFormValues.GuidValue(f,"workflow_definition_id"),CreateFormValues.IntValue(f,"version"),CreateFormValues.BoolValue(f,"publish")), ct);
        return Results.Redirect(Prefix + "/WorkflowVersionResponse/designer/" + created.id);
    }
    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: PublishAsync, GetDesignerAsync, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<WorkflowVersionResponse> Publish(Guid id, IWorkflowVersionService service, CancellationToken ct) => service.PublishAsync(id, ct);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: GetDesignerAsync, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<WorkflowDesignerResponse> Designer(Guid id, IWorkflowVersionService service, CancellationToken ct)
        => service.GetDesignerAsync(id, ct);

    public static async Task<IResult> CreateTestRequest(Guid id, IRequestService service, CancellationToken ct)
    {
        var request = await service.CreateTestRequestForWorkflowVersionAsync(id, ct);
        return Results.Redirect($"{Prefix}/RequestResponse/detail/{request.id}");
    }

    private static string? Optional(Microsoft.AspNetCore.Http.IFormCollection f, string name)
        => CreateFormValues.Optional(f, name);
    private static bool Checked(Microsoft.AspNetCore.Http.IFormCollection f, string name)
        => CreateFormValues.BoolValue(f, name);
    private static string Url(Guid id) => $"{Prefix}/WorkflowVersionResponse/designer/{id}";

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: CreateContainedAsync, UpdateDefinitionAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> AddState(HttpContext http, IWorkflowStateService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = CreateFormValues.GuidValue(f, "workflow_version_id");
        await service.CreateContainedAsync(new CreateWorkflowStateRequest(versionId,
            CreateFormValues.Required(f, "key"), CreateFormValues.Required(f, "name"),
            Optional(f, "description"), Checked(f, "is_initial"), Checked(f, "is_terminal"),
            CreateFormValues.IntValue(f, "position")), ct);
        return Results.Redirect(Url(versionId));
    }

    /// <summary>
    /// Zpracovává změnu existující entity; ověření oprávnění a integrity musí probíhat ve službě, ne pouze v UI. Volání: UpdateDefinitionAsync, DeleteDefinitionAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> UpdateState(HttpContext http, IWorkflowStateService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = CreateFormValues.GuidValue(f, "workflow_version_id");
        await service.UpdateDefinitionAsync(CreateFormValues.GuidValue(f, "id"),
            CreateFormValues.Required(f, "key"), CreateFormValues.Required(f, "name"),
            Optional(f, "description"), Checked(f, "is_initial"), Checked(f, "is_terminal"),
            CreateFormValues.IntValue(f, "position"), ct);
        return Results.Redirect(Url(versionId));
    }

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: DeleteDefinitionAsync, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DeleteState(HttpContext http, IWorkflowStateService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        await service.DeleteDefinitionAsync(CreateFormValues.GuidValue(f, "id"), ct);
        return Results.Redirect(Url(CreateFormValues.GuidValue(f, "workflow_version_id")));
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> AddTransition(HttpContext http, IWorkflowTransitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = CreateFormValues.GuidValue(f, "workflow_version_id");
        await service.CreateContainedAsync(new CreateWorkflowTransitionRequest(versionId,
            CreateFormValues.GuidValue(f, "source_state_id"),
            CreateFormValues.GuidValue(f, "target_state_id"),
            CreateFormValues.Required(f, "key"), CreateFormValues.Required(f, "name"),
            Checked(f, "note_required"), Optional(f, "condition_query"), Optional(f, "action_query")), ct);
        return Results.Redirect(Url(versionId));
    }

    /// <summary>
    /// Zpracovává změnu existující entity; ověření oprávnění a integrity musí probíhat ve službě, ne pouze v UI. Volání: UpdateDefinitionAsync, DeleteDefinitionAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> UpdateTransition(HttpContext http, IWorkflowTransitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = CreateFormValues.GuidValue(f, "workflow_version_id");
        await service.UpdateDefinitionAsync(CreateFormValues.GuidValue(f, "id"),
            CreateFormValues.GuidValue(f, "source_state_id"),
            CreateFormValues.GuidValue(f, "target_state_id"),
            CreateFormValues.Required(f, "key"), CreateFormValues.Required(f, "name"),
            Checked(f, "note_required"), Optional(f, "condition_query"), Optional(f, "action_query"), ct);
        return Results.Redirect(Url(versionId));
    }

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: DeleteDefinitionAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DeleteTransition(HttpContext http, IWorkflowTransitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        await service.DeleteDefinitionAsync(CreateFormValues.GuidValue(f, "id"), ct);
        return Results.Redirect(Url(CreateFormValues.GuidValue(f, "workflow_version_id")));
    }

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowVersionResponse").WithTags("DigitalProcesses.Workflow");
        group.MapActions<WorkflowVersionModel>("WorkflowVersionResponse");
        group.MapGet("/designer/{id:guid}", WorkflowVersionResolvers.Designer)
            .ApplyViewModel<WorkflowDesignerPage, WorkflowDesignerResponse>()
            .WithDescription("Opens the workflow graph editor with its states and transitions. Published versions are read-only.");
        group.MapPost("/designer/test/{id:guid}", WorkflowVersionResolvers.CreateTestRequest)
            .DisableAntiforgery()
            .WithDescription("Creates a test Request using the newest process version assigned to this exact workflow version, then redirects to the Request history.");
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
            .ApplyViewModel<WorkflowVersionModelPage,WorkflowVersionResponse>()
            .WithDescription("""
                Returns one WorkflowVersion visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", WorkflowVersionResolvers.List)
            .ApplyViewModel<WorkflowVersionListPage,WorkflowVersionResponse[]>()
            .WithDescription("""
                Returns WorkflowVersion entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", WorkflowVersionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-contained", WorkflowVersionResolvers.CreateApi)
            .ApplyJsonModel<WorkflowVersionModelPage,WorkflowVersionResponse>()
            .WithDescription("""
                Creates WorkflowVersion using parent WorkflowDefinition. Optional states and transitions supply the complete graph with source_key/target_key references; otherwise the previous version is cloned. The authorization target is derived server-side.
                """);

        group.MapPut("/definition/{id:guid}", WorkflowVersionResolvers.UpdateApi)
            .Produces<WorkflowDesignerResponse>()
            .WithDescription("Atomically replaces the graph of a draft workflow version. Items match by key; omitted transitions are deleted before omitted states. References from requests, history or access rules prevent deletion.");

        group.MapGet("/create", WorkflowVersionResolvers.CreatePage)
            .ApplyViewModel<WorkflowVersionCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create WorkflowVersion.
                """);
        group.MapPost("/create-form", WorkflowVersionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates WorkflowVersion from an HTML form submission and redirects to its detail page.
                """);
        group.MapPost("/publish/{id:guid}", WorkflowVersionResolvers.Publish)
            .ApplyJsonModel<WorkflowVersionModelPage,WorkflowVersionResponse>()
            .WithDescription("""
                Publishes a workflow version and freezes its state graph.
                """);
    }
}
