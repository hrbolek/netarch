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
using WebAppArchitecture.DigitalProcesses.UI.Designer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

/// <summary>
/// HTTP resolver pro Form Version Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class FormVersionResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, CreateContainedAsync, PublishAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IFormVersionService service, CancellationToken ct) => BaseResolvers.Read<FormVersionModel,FormVersionResponse,IFormVersionService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateContainedAsync, PublishAsync, GetTreeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IFormVersionService service, CancellationToken ct) => BaseResolvers.List<FormVersionModel,FormVersionResponse,IFormVersionService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateContainedAsync, PublishAsync, GetTreeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IFormVersionService service, CancellationToken ct) => BaseResolvers.Link<FormVersionModel,FormVersionResponse,IFormVersionService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, PublishAsync, GetTreeAsync, GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page("Create form version", Prefix+"/FormVersionResponse/create-form", Prefix+"/FormVersionResponse/list", "Creates a draft version of an existing form definition.", DigitalProcessCreateForm.GuidField("form_definition_id","Form definition ID"), DigitalProcessCreateForm.Number("version","Version","1","1"), DigitalProcessCreateForm.Check("publish","Publish immediately"));

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, PublishAsync, GetTreeAsync, GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<FormVersionResponse> CreateApi(CreateFormVersionRequest request, IFormVersionService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, PublishAsync, GetTreeAsync, GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IFormVersionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateFormVersionRequest(DigitalProcessCreateForm.GuidValue(f,"form_definition_id"),DigitalProcessCreateForm.IntValue(f,"version"),DigitalProcessCreateForm.BoolValue(f,"publish")), ct);
        return Results.Redirect(Prefix + "/FormVersionResponse/designer/" + created.id);
    }
    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: PublishAsync, GetTreeAsync, GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<FormVersionResponse> Publish(Guid id, IFormVersionService service, CancellationToken ct) => service.PublishAsync(id, ct);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: GetTreeAsync, GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<FormTreeResponse> Tree(Guid id, IFormNodeService service, CancellationToken ct) => service.GetTreeAsync(id, ct);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static async Task<FormDesignerPageResponse> Designer(Guid id, IFormNodeService service, CancellationToken ct)
        => new(await service.GetDesignerAsync(id, ct), false);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: GetDesignerAsync, ReadFormAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static async Task<FormDesignerPageResponse> Test(Guid id, IFormNodeService service, CancellationToken ct)
        => new(await service.GetDesignerAsync(id, ct), true);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: CreateContainedInVersionAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DesignerAddForm(HttpContext http, IFormNodeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = DigitalProcessCreateForm.GuidValue(f, "form_version_id");
        var request = new CreateFormNodeRequest(
            versionId,
            DigitalProcessCreateForm.Required(f, "key"),
            DigitalProcessCreateForm.GuidValue(f, "node_type_id"),
            DigitalProcessCreateForm.OptionalGuid(f, "parent_id"),
            DigitalProcessCreateForm.Optional(f, "label"),
            null,
            DigitalProcessCreateForm.IntValue(f, "position"),
            configuration_json: DigitalProcessCreateForm.GuidValue(f, "node_type_id") == DigitalProcessWellKnown.FormNodeTypes.Import
                ? DigitalProcessCreateForm.Optional(f, "configuration_json") : null);
        if (request.parent_id is null)
            await service.CreateContainedInVersionAsync(request, ct);
        else
            await service.CreateContainedInNodeAsync(request, ct);
        return Results.Redirect($"{Prefix}/FormVersionResponse/designer/{versionId}");
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: UpdateDefinitionAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DesignerUpdateForm(HttpContext http, IFormNodeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var id = DigitalProcessCreateForm.GuidValue(f, "id");
        var versionId = DigitalProcessCreateForm.GuidValue(f, "form_version_id");
        await service.UpdateDefinitionAsync(id, new UpdateFormNodeRequest(
            DigitalProcessCreateForm.Required(f, "key"),
            DigitalProcessCreateForm.Optional(f, "label"),
            DigitalProcessCreateForm.Optional(f, "description"),
            DigitalProcessCreateForm.Optional(f, "configuration_json"),
            DigitalProcessCreateForm.IntValue(f, "min_occurs"),
            DigitalProcessCreateForm.IntValue(f, "max_occurs")), ct);
        return Results.Redirect($"{Prefix}/FormVersionResponse/designer/{versionId}#node-{id}");
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: MoveAsync, ImportSubtreeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DesignerMoveForm(HttpContext http, IFormNodeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var id = DigitalProcessCreateForm.GuidValue(f, "id");
        var versionId = DigitalProcessCreateForm.GuidValue(f, "form_version_id");
        await service.MoveAsync(id, new MoveFormNodeRequest(
            DigitalProcessCreateForm.OptionalGuid(f, "parent_id"),
            DigitalProcessCreateForm.IntValue(f, "position")), ct);
        return Results.Redirect($"{Prefix}/FormVersionResponse/designer/{versionId}#node-{id}");
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: ImportSubtreeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DesignerImportForm(HttpContext http, IFormNodeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var versionId = DigitalProcessCreateForm.GuidValue(f, "target_form_version_id");
        await service.ImportSubtreeAsync(new ImportFormSubtreeRequest(
            DigitalProcessCreateForm.GuidValue(f, "source_node_id"),
            versionId,
            DigitalProcessCreateForm.OptionalGuid(f, "target_parent_id"),
            DigitalProcessCreateForm.Optional(f, "key_prefix"),
            0), ct);
        return Results.Redirect($"{Prefix}/FormVersionResponse/designer/{versionId}");
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: DeleteSubtreeAsync, UpsertFormTreeAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> DesignerDeleteForm(HttpContext http, IFormNodeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var id = DigitalProcessCreateForm.GuidValue(f, "id");
        var versionId = DigitalProcessCreateForm.GuidValue(f, "form_version_id");
        await service.DeleteSubtreeAsync(id, ct);
        return Results.Redirect($"{Prefix}/FormVersionResponse/designer/{versionId}");
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: UpsertFormTreeAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<FormTreeResponse> BulkTree(Guid id, BulkFormTreeRequest request, IBulkTreeService service, CancellationToken ct)
        => service.UpsertFormTreeAsync(id, request, ct);

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormVersionResponse").WithTags("DigitalProcesses.Forms");
        group.MapActions<FormVersionModel>("FormVersionResponse");
        group.MapGet("/view/{id:guid}", FormVersionResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<FormVersionResponse>,FormVersionResponse>()
            .WithDescription("""
                Returns one FormVersion visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", FormVersionResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<FormVersionResponse>,FormVersionResponse[]>()
            .WithDescription("""
                Returns FormVersion entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", FormVersionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-contained", FormVersionResolvers.CreateApi)
            .ApplyJsonModel<DigitalProcessModelPage<FormVersionResponse>,FormVersionResponse>()
            .WithDescription("""
                Creates FormVersion using parent FormDefinition. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", FormVersionResolvers.CreatePage)
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create FormVersion.
                """);
        group.MapPost("/create-form", FormVersionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates FormVersion from an HTML form submission and redirects to its detail page.
                """);
        group.MapPost("/publish/{id:guid}", FormVersionResolvers.Publish)
            .ApplyJsonModel<DigitalProcessModelPage<FormVersionResponse>,FormVersionResponse>()
            .WithDescription("""
                Publishes a draft form version and makes its structure immutable.
                """);

        group.MapGet("/designer/{id:guid}", FormVersionResolvers.Designer)
            .ApplyViewModel<FormDesignerPage,FormDesignerPageResponse>()
            .WithDescription("""
                Opens the draft form designer. The form canvas uses the same node renderers as runtime.
                """);
        group.MapGet("/test/{id:guid}", FormVersionResolvers.Test)
            .ApplyViewModel<FormDesignerPage,FormDesignerPageResponse>()
            .WithDescription("""
                Opens interactive test mode using runtime renderers and non-persistent dummy browser values. No Submission is created.
                """);
        group.MapPost("/designer/add-form", FormVersionResolvers.DesignerAddForm)
            .DisableAntiforgery()
            .WithDescription("Adds a root or child FormNode to a draft form version from the designer.");
        group.MapPost("/designer/update-form", FormVersionResolvers.DesignerUpdateForm)
            .DisableAntiforgery()
            .WithDescription("Updates editable FormNode definition properties from the designer.");
        group.MapPost("/designer/delete-form", FormVersionResolvers.DesignerDeleteForm)
            .DisableAntiforgery()
            .WithDescription("Deletes a node and its entire subtree from an editable draft; checks delete permission for every descendant.");
        group.MapPost("/designer/move-form", FormVersionResolvers.DesignerMoveForm)
            .DisableAntiforgery()
            .WithDescription("Moves a FormNode in the draft tree and recalculates its materialized subtree paths.");
        group.MapPost("/designer/import-form", FormVersionResolvers.DesignerImportForm)
            .DisableAntiforgery()
            .WithDescription("Deep-clones a reusable FormNode subtree into the target draft form version.");

        group.MapPut("/tree/{id:guid}", FormVersionResolvers.BulkTree)
            .WithDescription("Atomically create/update supplied form nodes. Omitted nodes are preserved; publishing locks the form. Authorization is verified per existing node.");
        group.MapGet("/tree/{id:guid}", FormVersionResolvers.Tree)
            .ApplyViewModel<FormTreePage,FormTreeResponse>()
            .WithDescription("""
                Returns the complete homogeneous form-node tree for the selected form version.
                """);
    }
}
