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
    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page("Create form node", Prefix+"/FormNodeResponse/create-form", Prefix+"/FormNodeResponse/list", "Adds a node to a draft form-version tree. The materialized path is computed by the service.", DigitalProcessCreateForm.GuidField("form_version_id","Form version ID"), DigitalProcessCreateForm.Text("key","Key"), DigitalProcessCreateForm.GuidField("node_type_id","Node type ID"), DigitalProcessCreateForm.GuidField("parent_id","Parent node ID",false), DigitalProcessCreateForm.Text("label","Label",false), DigitalProcessCreateForm.TextArea("description","Description",false), DigitalProcessCreateForm.Number("position","Position","0"), DigitalProcessCreateForm.Number("min_occurs","Min occurs","1","0"), DigitalProcessCreateForm.Number("max_occurs","Max occurs","1","1"), DigitalProcessCreateForm.Check("own_security_scope","Own security scope"), DigitalProcessCreateForm.TextArea("configuration_json","Configuration JSON",false));

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedInVersionAsync, CreateContainedInNodeAsync, MoveAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<FormNodeResponse> CreateInVersion(CreateFormNodeRequest request, IFormNodeService service, CancellationToken ct)
        => service.CreateContainedInVersionAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedInNodeAsync, CreateContainedInVersionAsync, MoveAsync, GetSubtreeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<FormNodeResponse> CreateInNode(CreateFormNodeRequest request, IFormNodeService service, CancellationToken ct)
        => service.CreateContainedInNodeAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedInVersionAsync, CreateContainedInNodeAsync, MoveAsync, GetSubtreeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IFormNodeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var request = new CreateFormNodeRequest(DigitalProcessCreateForm.GuidValue(f,"form_version_id"),DigitalProcessCreateForm.Required(f,"key"),DigitalProcessCreateForm.GuidValue(f,"node_type_id"),DigitalProcessCreateForm.OptionalGuid(f,"parent_id"),DigitalProcessCreateForm.Optional(f,"label"),DigitalProcessCreateForm.Optional(f,"description"),DigitalProcessCreateForm.IntValue(f,"position"),DigitalProcessCreateForm.IntValue(f,"min_occurs"),DigitalProcessCreateForm.IntValue(f,"max_occurs"),DigitalProcessCreateForm.BoolValue(f,"own_security_scope"),DigitalProcessCreateForm.Optional(f,"configuration_json"));
        var created = request.parent_id is null
            ? await service.CreateContainedInVersionAsync(request, ct)
            : await service.CreateContainedInNodeAsync(request, ct);
        return Results.Redirect(Prefix + "/FormNodeResponse/view/" + created.id);
    }
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: MoveAsync, GetSubtreeAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<FormNodeResponse> Move(Guid id, MoveFormNodeRequest request, IFormNodeService service, CancellationToken ct) => service.MoveAsync(id, request, ct);
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
        var group = endpoints.MapGroup(prefix + "/FormNodeResponse").WithTags("DigitalProcesses.Forms");
        group.MapActions<FormNodeModel>("FormNodeResponse");
        group.MapGet("/view/{id:guid}", FormNodeResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<FormNodeResponse>,FormNodeResponse>()
            .WithDescription("""
                Returns one FormNode visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", FormNodeResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<FormNodeResponse>,FormNodeResponse[]>()
            .WithDescription("""
                Returns FormNode entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", FormNodeResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-in-version", FormNodeResolvers.CreateInVersion)
            .ApplyJsonModel<DigitalProcessModelPage<FormNodeResponse>,FormNodeResponse>()
            .WithDescription("""
                Creates a root FormNode inside form_version_id. Create permission is evaluated on the existing FormVersion RBAC scope. parent_id is ignored and rbacobject_id cannot be supplied by the client.
                """);
        group.MapPost("/create-in-node", FormNodeResolvers.CreateInNode)
            .ApplyJsonModel<DigitalProcessModelPage<FormNodeResponse>,FormNodeResponse>()
            .WithDescription("""
                Creates a child FormNode below parent_id. Create permission is evaluated on the existing parent FormNode RBAC scope, and the service validates that parent and child belong to the same FormVersion. rbacobject_id cannot be supplied by the client.
                """);

        group.MapGet("/create", FormNodeResolvers.CreatePage)
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create FormNode.
                """);
        group.MapPost("/create-form", FormNodeResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates FormNode from an HTML form submission and redirects to its detail page.
                """);
        group.MapPut("/move/{id:guid}", FormNodeResolvers.Move)
            .ApplyJsonModel<DigitalProcessModelPage<FormNodeResponse>,FormNodeResponse>()
            .WithDescription("""
                Moves a form node and recalculates materialized paths for its subtree.
                """);
        group.MapGet("/subtree/{id:guid}", FormNodeResolvers.Subtree)
            .ApplyJsonModel<DigitalProcessListPage<FormNodeResponse>,FormNodeResponse[]>()
            .WithDescription("""
                Returns the selected form node and all descendants selected by materialized path.
                """);
    }
}
