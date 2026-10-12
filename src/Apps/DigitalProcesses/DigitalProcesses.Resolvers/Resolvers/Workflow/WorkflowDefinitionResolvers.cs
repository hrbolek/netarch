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

/// <summary>
/// HTTP resolver pro Workflow Definition Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class WorkflowDefinitionResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IWorkflowDefinitionService service, CancellationToken ct) => BaseResolvers.Read<WorkflowDefinitionModel,WorkflowDefinitionResponse,IWorkflowDefinitionService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IWorkflowDefinitionService service, CancellationToken ct) => BaseResolvers.List<WorkflowDefinitionModel,WorkflowDefinitionResponse,IWorkflowDefinitionService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IWorkflowDefinitionService service, CancellationToken ct) => BaseResolvers.Link<WorkflowDefinitionModel,WorkflowDefinitionResponse,IWorkflowDefinitionService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, GetByIdAsync, Query, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page("Create workflow definition", Prefix+"/WorkflowDefinitionResponse/create-form", Prefix+"/WorkflowDefinitionResponse/list", "Creates the stable identity of a workflow.", DigitalProcessCreateForm.Text("name","Name"),DigitalProcessCreateForm.TextArea("description","Description",false));

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, GetByIdAsync, Query, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<WorkflowDefinitionResponse> CreateApi(CreateWorkflowDefinitionRequest request, IWorkflowDefinitionService service, CancellationToken ct)
        => service.CreateTypedAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, GetByIdAsync, Query, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IWorkflowDefinitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateTypedAsync(new CreateWorkflowDefinitionRequest(DigitalProcessCreateForm.Required(f,"name"),DigitalProcessCreateForm.Optional(f,"description")), ct);
        return Results.Redirect(Prefix + "/WorkflowDefinitionResponse/view/" + created.id);
    }

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: PublishAsync, GetByIdAsync, Query; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<DefinitionVersionsPageModel> Detail(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        var definition = await security.GetByIdAsync<WorkflowDefinitionModel>(id, SecurityOperations.Read, ct) ?? throw new KeyNotFoundException("Definition not found.");
        var versions = security.Query<WorkflowVersionModel>(SecurityOperations.Read).Where(v => v.workflow_definition_id == id).OrderByDescending(v => v.version)
            .Select(v => new DefinitionVersionItem(v.id, v.version, v.is_published, v.updated_at)).ToArray();
        return new DefinitionVersionsPageModel(id, definition.name, "Workflow", versions, WorkflowDefinitionResponse.FromModel(definition));
    }
    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: PublishAsync, CreateContainedAsync, DeleteDraftAsync, GetByIdAsync, Query; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> PublishVersion(Guid id, Guid definitionId, IWorkflowVersionService versions, CancellationToken ct)
    {
        var published = await versions.PublishAsync(id, ct);
        if (published.workflow_definition_id != definitionId) return Results.BadRequest();
        return Results.Redirect($"/digital-processes/WorkflowDefinitionResponse/view/{definitionId}");
    }
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: CreateContainedAsync, DeleteDraftAsync, GetByIdAsync, Query; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> NewVersion(Guid id, ISecurityLayer security, IWorkflowVersionService versions, CancellationToken ct)
    {
        _ = await security.GetByIdAsync<WorkflowDefinitionModel>(id, SecurityOperations.Read, ct) ?? throw new KeyNotFoundException();
        var next = security.Query<WorkflowVersionModel>(SecurityOperations.Read).Where(v => v.workflow_definition_id == id).Select(v => v.version).ToArray().DefaultIfEmpty(0).Max() + 1;
        var created = await versions.CreateContainedAsync(new CreateWorkflowVersionRequest(id, next), ct);
        return Results.Redirect($"/digital-processes/WorkflowVersionResponse/designer/{created.id}");
    }
    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: DeleteDraftAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DeleteDraft(Guid id, Guid definitionId, IWorkflowVersionService versions, CancellationToken ct)
    {
        await versions.DeleteDraftAsync(id, ct);
        return Results.Redirect("/digital-processes/WorkflowDefinitionResponse/view/" + definitionId);
    }
    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowDefinitionResponse").WithTags("DigitalProcesses.Workflow");
        group.MapActions<WorkflowDefinitionModel>("WorkflowDefinitionResponse");
        group.MapPost("/delete-version/{id:guid}", DeleteDraft)
            .WithDescription("Deletes an unpublished version and its dependent records if no runtime or process references exist.");
        group.MapPost("/publish-version/{id:guid}", PublishVersion).WithDescription("Publishes one draft version through RBAC-checked services and returns to the parent definition.");
        group.MapPost("/new-version/{id:guid}", NewVersion).WithDescription("Creates a new workflow draft version from a workflow definition with an automatically incremented version number.");
        group.MapGet("/view/{id:guid}", WorkflowDefinitionResolvers.Detail)
            .ApplyViewModel<DefinitionVersionsPage,DefinitionVersionsPageModel>()
            .WithDescription("""
                Returns one WorkflowDefinition visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", WorkflowDefinitionResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<WorkflowDefinitionResponse>,WorkflowDefinitionResponse[]>()
            .WithDescription("""
                Returns WorkflowDefinition entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", WorkflowDefinitionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-typed", WorkflowDefinitionResolvers.CreateApi)
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowDefinitionResponse>,WorkflowDefinitionResponse>()
            .WithDescription("""
                Creates WorkflowDefinition using typed creation scope for WorkflowDefinition roots. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", WorkflowDefinitionResolvers.CreatePage)
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create WorkflowDefinition.
                """);
        group.MapPost("/create-form", WorkflowDefinitionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates WorkflowDefinition from an HTML form submission and redirects to its detail page.
                """);
    }
}
