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

public static class FormVersionResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IFormVersionService service, CancellationToken ct) => BaseResolvers.Read<FormVersionModel,FormVersionResponse,IFormVersionService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IFormVersionService service, CancellationToken ct) => BaseResolvers.List<FormVersionModel,FormVersionResponse,IFormVersionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IFormVersionService service, CancellationToken ct) => BaseResolvers.Link<FormVersionModel,FormVersionResponse,IFormVersionService>(id,http,service,ct);

    public static RazorComponentResult<DigitalProcessCreatePage> CreatePage() => DigitalProcessCreateForm.Page("Create form version", Prefix+"/FormVersionResponse/create-form", Prefix+"/FormVersionResponse/list", "Creates a draft version of an existing form definition.", DigitalProcessCreateForm.GuidField("form_definition_id","Form definition ID"), DigitalProcessCreateForm.Number("version","Version","1","1"), DigitalProcessCreateForm.Check("publish","Publish immediately"));

    public static Task<FormVersionResponse> CreateApi(CreateFormVersionRequest request, IFormVersionService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IFormVersionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateFormVersionRequest(DigitalProcessCreateForm.GuidValue(f,"form_definition_id"),DigitalProcessCreateForm.IntValue(f,"version"),DigitalProcessCreateForm.BoolValue(f,"publish")), ct);
        return Results.Redirect(Prefix + "/FormVersionResponse/view/" + created.id);
    }
    public static Task<FormVersionResponse> Publish(Guid id, IFormVersionService service, CancellationToken ct) => service.PublishAsync(id, ct);
    public static Task<FormTreeResponse> Tree(Guid id, IFormNodeService service, CancellationToken ct) => service.GetTreeAsync(id, ct);

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormVersionResponse").WithTags("DigitalProcesses.Forms");
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
        group.MapGet("/tree/{id:guid}", FormVersionResolvers.Tree)
            .ApplyViewModel<FormTreePage,FormTreeResponse>()
            .WithDescription("""
                Returns the complete homogeneous form-node tree for the selected form version.
                """);
    }
}
