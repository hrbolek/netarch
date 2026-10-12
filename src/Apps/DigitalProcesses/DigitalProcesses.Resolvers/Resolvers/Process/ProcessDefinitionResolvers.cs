using WebAppArchitecture.DigitalProcesses.DBLayer;
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
/// HTTP resolver pro Process Definition Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class ProcessDefinitionResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, CreateTypedAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IProcessDefinitionService service, CancellationToken ct) => BaseResolvers.Read<ProcessDefinitionModel,ProcessDefinitionResponse,IProcessDefinitionService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateTypedAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IProcessDefinitionService service, CancellationToken ct) => BaseResolvers.List<ProcessDefinitionModel,ProcessDefinitionResponse,IProcessDefinitionService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IProcessDefinitionService service, CancellationToken ct) => BaseResolvers.Link<ProcessDefinitionModel,ProcessDefinitionResponse,IProcessDefinitionService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page(
        "Create process definition",
        Prefix + "/ProcessDefinitionResponse/create-form",
        Prefix + "/ProcessDefinitionResponse/list",
        "Creates a process together with its own form, workflow, and first draft versions. The request initiator uses the system role type 'já'.",
        DigitalProcessCreateForm.Text("name", "Name"),
        DigitalProcessCreateForm.TextArea("description", "Description", false));

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, GetByIdAsync, Query, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<ProcessDefinitionResponse> CreateApi(CreateProcessDefinitionRequest request, IProcessDefinitionService service, CancellationToken ct)
        => service.CreateTypedAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, GetByIdAsync, Query, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IProcessDefinitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateTypedAsync(new CreateProcessDefinitionRequest(
            DigitalProcessCreateForm.Required(f, "name"),
            description: DigitalProcessCreateForm.Optional(f, "description")), ct);
        return Results.Redirect(Prefix + "/ProcessDefinitionResponse/view/" + created.id);
    }

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: PublishAsync, DeleteDraftAsync, GetByIdAsync, Query; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<DefinitionVersionsPageModel> Detail(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        var definition = await security.GetByIdAsync<ProcessDefinitionModel>(id, SecurityOperations.Read, ct) ?? throw new KeyNotFoundException("Definition not found.");
        var versions = security.Query<ProcessDefinitionVersionModel>(SecurityOperations.Read).Where(v => v.process_definition_id == id).OrderByDescending(v => v.version)
            .Select(v => new DefinitionVersionItem(v.id, v.version, v.is_published, v.updated_at)).ToArray();
        return new DefinitionVersionsPageModel(id, definition.name, "Process", versions, ProcessDefinition: ProcessDefinitionResponse.FromModel(definition));
    }
    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: PublishAsync, DeleteDraftAsync, CloneLatestAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> PublishVersion(Guid id, Guid definitionId, IProcessDefinitionVersionService versions, CancellationToken ct)
    {
        var published = await versions.PublishAsync(id, ct);
        if (published.process_definition_id != definitionId) return Results.BadRequest();
        return Results.Redirect($"/digital-processes/ProcessDefinitionResponse/view/{definitionId}");
    }
    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: DeleteDraftAsync, CloneLatestAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DeleteDraft(Guid id, Guid definitionId, IProcessDefinitionVersionService versions, CancellationToken ct)
    {
        await versions.DeleteDraftAsync(id, ct);
        return Results.Redirect("/digital-processes/ProcessDefinitionResponse/view/" + definitionId);
    }
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: CloneLatestAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CloneVersion(Guid id, IProcessDefinitionVersionService versions, CancellationToken ct)
    {
        var created = await versions.CloneLatestAsync(id, ct);
        return Results.Redirect($"/digital-processes/ProcessDefinitionResponse/view/{id}");
    }
    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/ProcessDefinitionResponse").WithTags("DigitalProcesses.Process");
        group.MapActions<ProcessDefinitionModel>("ProcessDefinitionResponse");
        group.MapPost("/delete-version/{id:guid}", DeleteDraft)
            .WithDescription("Deletes an unpublished version and its dependent records if no runtime or process references exist.");
        group.MapPost("/new-version/{id:guid}", CloneVersion)
            .WithDescription("Creates a draft copy of the most recent process version, retaining form and workflow version references.");
        group.MapPost("/publish-version/{id:guid}", PublishVersion).WithDescription("Publishes one draft version through RBAC-checked services and returns to the parent definition.");
        group.MapGet("/view/{id:guid}", ProcessDefinitionResolvers.Detail)
            .ApplyViewModel<DefinitionVersionsPage,DefinitionVersionsPageModel>()
            .WithDescription("""
                Returns one ProcessDefinition visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", ProcessDefinitionResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<ProcessDefinitionResponse>,ProcessDefinitionResponse[]>()
            .WithDescription("""
                Returns ProcessDefinition entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", ProcessDefinitionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-typed", ProcessDefinitionResolvers.CreateApi)
            .ApplyJsonModel<DigitalProcessModelPage<ProcessDefinitionResponse>,ProcessDefinitionResponse>()
            .WithDescription("""
                Atomically creates ProcessDefinition, its owned FormDefinition and WorkflowDefinition, and all three first draft versions using typed creation scope. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", ProcessDefinitionResolvers.CreatePage)
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create ProcessDefinition.
                """);
        group.MapPost("/create-form", ProcessDefinitionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates ProcessDefinition from an HTML form submission and redirects to its detail page.
                """);
    }
}
