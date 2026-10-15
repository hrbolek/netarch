using WebAppArchitecture.DigitalProcesses.UI.FormNodeType.Components;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeType;
using WebAppArchitecture.DigitalProcesses.UI.FormNodeType.Pages;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.FormNodeType;
/// <summary>
/// HTTP resolver pro Form Node Type Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class FormNodeTypeResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, CreateTypedAsync, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IFormNodeTypeService service, CancellationToken ct) => BaseResolvers.Read<FormNodeTypeModel,FormNodeTypeResponse,IFormNodeTypeService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateTypedAsync, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IFormNodeTypeService service, CancellationToken ct) => BaseResolvers.List<FormNodeTypeModel,FormNodeTypeResponse,IFormNodeTypeService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateTypedAsync, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IFormNodeTypeService service, CancellationToken ct) => BaseResolvers.Link<FormNodeTypeModel,FormNodeTypeResponse,IFormNodeTypeService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => FormNodeTypeCreateForm.CreatePage();

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<FormNodeTypeResponse> CreateTyped(CreateFormNodeTypeRequest request, IFormNodeTypeService service, CancellationToken ct)
        => service.CreateTypedAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, CreateTypedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<FormNodeTypeResponse> CreateContained(CreateFormNodeTypeRequest request, IFormNodeTypeService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateTypedAsync, CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IFormNodeTypeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var request = new CreateFormNodeTypeRequest(CreateFormValues.Required(f,"name"),CreateFormValues.Required(f,"renderer_key"),CreateFormValues.BoolValue(f,"can_have_children"),CreateFormValues.BoolValue(f,"can_have_value"),CreateFormValues.BoolValue(f,"can_repeat"),CreateFormValues.OptionalGuid(f,"parent_type_id"),CreateFormValues.Optional(f,"configuration_schema_json"));
        var created = request.parent_type_id is null
            ? await service.CreateTypedAsync(request, ct)
            : await service.CreateContainedAsync(request, ct);
        return Results.Redirect(Prefix + "/FormNodeTypeResponse/view/" + created.id);
    }

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormNodeTypeResponse").WithTags("DigitalProcesses.Forms");
        group.MapActions<FormNodeTypeModel>("FormNodeTypeResponse");
        group.MapGet("/view/{id:guid}", FormNodeTypeResolvers.Read)
            .ApplyViewModel<FormNodeTypeModelPage,FormNodeTypeResponse>()
            .WithDescription("""
                Returns one FormNodeType visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", FormNodeTypeResolvers.List)
            .ApplyViewModel<FormNodeTypeListPage,FormNodeTypeResponse[]>()
            .WithDescription("""
                Returns FormNodeType entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", FormNodeTypeResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-typed", FormNodeTypeResolvers.CreateTyped)
            .ApplyJsonModel<FormNodeTypeModelPage,FormNodeTypeResponse>()
            .WithDescription("""
                Creates a root FormNodeType under the system FormNodeType creation scope. The client cannot choose an RBAC target.
                """);
        group.MapPost("/create-contained", FormNodeTypeResolvers.CreateContained)
            .ApplyJsonModel<FormNodeTypeModelPage,FormNodeTypeResponse>()
            .WithDescription("""
                Creates a child FormNodeType under parent_type_id. Create permission is evaluated on the parent type's RBAC scope.
                """);

        group.MapGet("/create", FormNodeTypeResolvers.CreatePage)
            .ApplyViewModel<FormNodeTypeCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create FormNodeType.
                """);
        group.MapPost("/create-form", FormNodeTypeResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates FormNodeType from an HTML form submission and redirects to its detail page.
                """);
    }
}
