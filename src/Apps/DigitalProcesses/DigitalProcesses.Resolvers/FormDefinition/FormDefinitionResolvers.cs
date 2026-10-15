using WebAppArchitecture.DigitalProcesses.UI.FormDefinition.Components;
using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;
using WebAppArchitecture.DigitalProcesses.UI.FormDefinition.Pages;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System.Net;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.SecurityLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormDefinition;
/// <summary>
/// HTTP resolver pro Form Definition Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class FormDefinitionResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, GetDetailAsync, CreateTypedAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<FormDefinitionDetailResponse> Read(Guid id, IFormDefinitionService service, CancellationToken ct)
        => service.GetDetailAsync(id, ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IFormDefinitionService service, CancellationToken ct) => BaseResolvers.List<FormDefinitionModel,FormDefinitionResponse,IFormDefinitionService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IFormDefinitionService service, CancellationToken ct) => BaseResolvers.Link<FormDefinitionModel,FormDefinitionResponse,IFormDefinitionService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => FormDefinitionCreateForm.CreatePage();

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateApi(CreateFormDefinitionRequest request, IFormDefinitionService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.CreateTypedAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    // This page form is a browser navigation, so an authorization failure must be
    // understandable HTML with HTTP 403 rather than a raw ASP.NET exception page.
    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IFormDefinitionService service, ILoggerFactory loggerFactory, CancellationToken ct)
    {
        try
        {
            var f = await http.Request.ReadFormAsync(ct);
            var created = await service.CreateTypedAsync(new CreateFormDefinitionRequest(CreateFormValues.Required(f,"name"),CreateFormValues.Optional(f,"description")), ct);
            return Results.Redirect(Prefix + "/FormDefinitionResponse/view/" + created.id);
        }
        catch (UnauthorizedAccessException ex)
        {
            loggerFactory.CreateLogger("FormDefinition.CreateForm").LogWarning(ex,
                "Form definition creation denied for actor {Actor}; required scope {Scope}",
                http.User.FindFirst("oid")?.Value ?? http.User.Identity?.Name ?? "unknown",
                RbacWellKnown.FormDefinitionCreationScopeId);
            return Results.Content(FormCreateAuthorizationMessage.Html, "text/html; charset=utf-8", statusCode: StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: IsAllowedAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreatePermission(IFormDefinitionService service, CancellationToken ct)
    {
        try
        {
            var allowed = await service.CanCreateAsync(ct);
            return Results.Ok(new {
                allowed,
                operation = SecurityOperations.Create,
                scope_id = RbacWellKnown.FormDefinitionCreationScopeId,
                scope_name = "Create Form Definitions",
                guidance = allowed ? "Form definition creation is permitted." : FormCreateAuthorizationMessage.Guidance
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Json(new {
                allowed = false,
                operation = SecurityOperations.Create,
                scope_id = RbacWellKnown.FormDefinitionCreationScopeId,
                guidance = FormCreateAuthorizationMessage.Guidance
            }, statusCode: StatusCodes.Status403Forbidden);
        }
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: CreateContainedAsync, PublishAsync, DeleteDraftAsync, Query; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> NewVersion(Guid id, IFormVersionService versions, CancellationToken ct)
    {
        var created = await versions.CreateNextAsync(id, ct);
        return Results.Redirect($"/digital-processes/FormVersionResponse/designer/{created.id}");
    }
    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: PublishAsync, DeleteDraftAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> PublishVersion(Guid id, Guid definitionId, IFormVersionService versions, CancellationToken ct)
    {
        var published = await versions.PublishAsync(id, ct);
        if (published.form_definition_id != definitionId) return Results.BadRequest();
        return Results.Redirect($"/digital-processes/FormDefinitionResponse/view/{definitionId}");
    }
    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: DeleteDraftAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DeleteDraft(Guid id, Guid definitionId, IFormVersionService versions, CancellationToken ct)
    {
        await versions.DeleteDraftAsync(id, ct);
        return Results.Redirect("/digital-processes/FormDefinitionResponse/view/" + definitionId);
    }
    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormDefinitionResponse").WithTags("DigitalProcesses.Forms");
        group.MapActions<FormDefinitionModel>("FormDefinitionResponse");
        group.MapPost("/delete-version/{id:guid}", DeleteDraft)
            .WithDescription("Deletes an unpublished version and its dependent records if no runtime or process references exist.");
        group.MapPost("/new-version/{id:guid}", NewVersion).WithDescription("Creates the next draft form version from its parent definition, with server-assigned version number and RBAC checks.");
        group.MapPost("/publish-version/{id:guid}", PublishVersion).WithDescription("Publishes a draft form version and returns to the owning form definition.");
        group.MapGet("/view/{id:guid}", FormDefinitionResolvers.Read)
            .ApplyViewModel<FormDefinitionDetailPage,FormDefinitionDetailResponse>()
            .WithDescription("""
                Returns one FormDefinition together with all readable versions. The HTML view exposes draft Designer/Test actions.
                """);
        group.MapGet("/list", FormDefinitionResolvers.List)
            .ApplyViewModel<FormDefinitionListPage,FormDefinitionResponse[]>()
            .WithDescription("""
                Returns FormDefinition entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", FormDefinitionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-typed", FormDefinitionResolvers.CreateApi)
            .ApplyJsonModel<FormDefinitionModelPage,FormDefinitionResponse>()
            .WithDescription("""
                Creates FormDefinition using typed creation scope for FormDefinition roots. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                Accepts name and optional description; an optional children object in the FormSchema renderer_key format creates the initial FormVersion tree in the same transaction.
                """);

        group.MapGet("/create", FormDefinitionResolvers.CreatePage)
            .ApplyViewModel<FormDefinitionCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create FormDefinition.
                """);
        group.MapGet("/create-permission", FormDefinitionResolvers.CreatePermission)
            .WithDescription("Checks effective Create permission on the well-known FormDefinition creation RBAC scope; returns the scope and administrator guidance.");
        group.MapPost("/create-form", FormDefinitionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates FormDefinition from an HTML form submission and redirects to its detail page.
                """);
    }
}
