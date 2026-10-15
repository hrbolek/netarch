using WebAppArchitecture.DigitalProcesses.UI.FormNode.Components;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.UI.FormNode.Pages;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormNode;
/// <summary>
/// HTTP resolver pro Form Node Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class FormNodeResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, CreateContainedInVersionAsync, CreateContainedInNodeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IFormNodeService service, CancellationToken ct) => BaseResolvers.Read<FormNodeModel,FormNodeResponse,IFormNodeService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateContainedInVersionAsync, CreateContainedInNodeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IFormNodeService service, CancellationToken ct) => BaseResolvers.List<FormNodeModel,FormNodeResponse,IFormNodeService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateContainedInVersionAsync, CreateContainedInNodeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IFormNodeService service, CancellationToken ct) => BaseResolvers.Link<FormNodeModel,FormNodeResponse,IFormNodeService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedInVersionAsync, CreateContainedInNodeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => FormNodeCreateForm.CreatePage();

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedInVersionAsync, CreateContainedInNodeAsync, MoveAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateInVersion(CreateFormNodeRequest request, IFormNodeService service, CancellationToken ct)
    {
        try
        {
            return ApiResults.Ok(await service.CreateContainedInVersionAsync(request, ct));
        }
        catch (System.Text.Json.JsonException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedInNodeAsync, CreateContainedInVersionAsync, MoveAsync, GetSubtreeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateInNode(CreateFormNodeRequest request, IFormNodeService service, CancellationToken ct)
    {
        try
        {
            return ApiResults.Ok(await service.CreateContainedInNodeAsync(request, ct));
        }
        catch (System.Text.Json.JsonException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedInVersionAsync, CreateContainedInNodeAsync, MoveAsync, GetSubtreeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IFormNodeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var request = new CreateFormNodeRequest(CreateFormValues.GuidValue(f,"form_version_id"),CreateFormValues.Required(f,"key"),CreateFormValues.GuidValue(f,"node_type_id"),CreateFormValues.OptionalGuid(f,"parent_id"),CreateFormValues.Optional(f,"label"),CreateFormValues.Optional(f,"description"),CreateFormValues.IntValue(f,"position"),CreateFormValues.IntValue(f,"min_occurs"),CreateFormValues.IntValue(f,"max_occurs"),CreateFormValues.BoolValue(f,"own_security_scope"),CreateFormValues.Optional(f,"configuration_json"));
        try
        {
            var created = request.parent_id is null
                ? await service.CreateContainedInVersionAsync(request, ct)
                : await service.CreateContainedInNodeAsync(request, ct);
            return Results.Redirect(Prefix + "/FormNodeResponse/view/" + created.id);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: MoveAsync, GetSubtreeAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static async Task<object> Move(Guid id, MoveFormNodeRequest request, IFormNodeService service, CancellationToken ct)
    {
        try { return await service.MoveAsync(id, request, ct); }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: GetSubtreeAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static async Task<FormNodeResponse[]> Subtree(Guid id, IFormNodeService service, CancellationToken ct)
        => (await service.GetSubtreeAsync(id, ct)).ToArray();

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        FormNodeEditorResolvers.Map(endpoints, prefix);
        var group = endpoints.MapGroup(prefix + "/FormNodeResponse").WithTags("DigitalProcesses.Forms");
        group.MapActions<FormNodeModel>("FormNodeResponse");
        group.MapGet("/view/{id:guid}", FormNodeResolvers.Read)
            .ApplyViewModel<FormNodeModelPage,FormNodeResponse>()
            .WithDescription("""
                Returns one FormNode visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", FormNodeResolvers.List)
            .ApplyViewModel<FormNodeListPage,FormNodeResponse[]>()
            .WithDescription("""
                Returns FormNode entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", FormNodeResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-in-version", FormNodeResolvers.CreateInVersion)
            .ApplyJsonModel<FormNodeModelPage,FormNodeResponse>()
            .WithDescription("""
                Creates a root FormNode inside form_version_id. Create permission is evaluated on the existing FormVersion RBAC scope. parent_id is ignored and rbacobject_id cannot be supplied by the client.
                """);
        group.MapPost("/create-in-node", FormNodeResolvers.CreateInNode)
            .ApplyJsonModel<FormNodeModelPage,FormNodeResponse>()
            .WithDescription("""
                Creates a child FormNode below parent_id. Create permission is evaluated on the existing parent FormNode RBAC scope, and the service validates that parent and child belong to the same FormVersion. rbacobject_id cannot be supplied by the client.
                """);

        group.MapGet("/create", FormNodeResolvers.CreatePage)
            .ApplyViewModel<FormNodeCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create FormNode.
                """);
        group.MapPost("/create-form", FormNodeResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates FormNode from an HTML form submission and redirects to its detail page.
                """);
        group.MapPut("/move/{id:guid}", FormNodeResolvers.Move)
            .ApplyJsonModel<FormNodeModelPage,FormNodeResponse>()
            .WithDescription("""
                Moves a form node and recalculates materialized paths for its subtree.
                """);
        group.MapGet("/subtree/{id:guid}", FormNodeResolvers.Subtree)
            .ApplyJsonModel<FormNodeListPage,FormNodeResponse[]>()
            .WithDescription("""
                Returns the selected form node and all descendants selected by materialized path.
                """);
    }
}
