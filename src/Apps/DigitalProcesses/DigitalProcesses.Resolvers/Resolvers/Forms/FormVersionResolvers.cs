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

public static class FormVersionResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IFormVersionService service, CancellationToken ct) => BaseResolvers.Read<FormVersionModel,FormVersionResponse,IFormVersionService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IFormVersionService service, CancellationToken ct) => BaseResolvers.List<FormVersionModel,FormVersionResponse,IFormVersionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IFormVersionService service, CancellationToken ct) => BaseResolvers.Link<FormVersionModel,FormVersionResponse,IFormVersionService>(id,http,service,ct);

    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page("Create form version", Prefix+"/FormVersionResponse/create-form", Prefix+"/FormVersionResponse/list", "Creates a draft version of an existing form definition.", DigitalProcessCreateForm.GuidField("form_definition_id","Form definition ID"), DigitalProcessCreateForm.Number("version","Version","1","1"), DigitalProcessCreateForm.Check("publish","Publish immediately"));

    public static Task<FormVersionResponse> CreateApi(CreateFormVersionRequest request, IFormVersionService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IFormVersionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateFormVersionRequest(DigitalProcessCreateForm.GuidValue(f,"form_definition_id"),DigitalProcessCreateForm.IntValue(f,"version"),DigitalProcessCreateForm.BoolValue(f,"publish")), ct);
        return Results.Redirect(Prefix + "/FormVersionResponse/designer/" + created.id);
    }
    public static Task<FormVersionResponse> Publish(Guid id, IFormVersionService service, CancellationToken ct) => service.PublishAsync(id, ct);
    public static Task<FormTreeResponse> Tree(Guid id, IFormNodeService service, CancellationToken ct) => service.GetTreeAsync(id, ct);

    public static async Task<FormDesignerPageResponse> Designer(Guid id, IFormNodeService service, CancellationToken ct)
        => new(await service.GetDesignerAsync(id, ct), false);

    public static async Task<FormDesignerPageResponse> Test(Guid id, IFormNodeService service, CancellationToken ct)
        => new(await service.GetDesignerAsync(id, ct), true);

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
            DigitalProcessCreateForm.IntValue(f, "position"));
        if (request.parent_id is null)
            await service.CreateContainedInVersionAsync(request, ct);
        else
            await service.CreateContainedInNodeAsync(request, ct);
        return Results.Redirect($"{Prefix}/FormVersionResponse/designer/{versionId}");
    }

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

    public static async Task<IResult> DesignerDeleteForm(HttpContext http, IFormNodeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var id = DigitalProcessCreateForm.GuidValue(f, "id");
        var versionId = DigitalProcessCreateForm.GuidValue(f, "form_version_id");
        await service.DeleteSubtreeAsync(id, ct);
        return Results.Redirect($"{Prefix}/FormVersionResponse/designer/{versionId}");
    }

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

        group.MapGet("/tree/{id:guid}", FormVersionResolvers.Tree)
            .ApplyViewModel<FormTreePage,FormTreeResponse>()
            .WithDescription("""
                Returns the complete homogeneous form-node tree for the selected form version.
                """);
    }
}
