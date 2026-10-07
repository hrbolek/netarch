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

public static class FormDefinitionResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IFormDefinitionService service, CancellationToken ct) => BaseResolvers.Read<FormDefinitionModel,FormDefinitionResponse,IFormDefinitionService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IFormDefinitionService service, CancellationToken ct) => BaseResolvers.List<FormDefinitionModel,FormDefinitionResponse,IFormDefinitionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IFormDefinitionService service, CancellationToken ct) => BaseResolvers.Link<FormDefinitionModel,FormDefinitionResponse,IFormDefinitionService>(id,http,service,ct);

    public static RazorComponentResult<DigitalProcessCreatePage> CreatePage() => DigitalProcessCreateForm.Page("Create form definition", Prefix+"/FormDefinitionResponse/create-form", Prefix+"/FormDefinitionResponse/list", "Creates the stable identity of a form. Form structure is edited in form versions.", DigitalProcessCreateForm.Text("name","Name"), DigitalProcessCreateForm.TextArea("description","Description",false));

    public static Task<FormDefinitionResponse> CreateApi(CreateFormDefinitionRequest request, IFormDefinitionService service, CancellationToken ct)
        => service.CreateTypedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IFormDefinitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateTypedAsync(new CreateFormDefinitionRequest(DigitalProcessCreateForm.Required(f,"name"),DigitalProcessCreateForm.Optional(f,"description")), ct);
        return Results.Redirect(Prefix + "/FormDefinitionResponse/view/" + created.id);
    }

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormDefinitionResponse").WithTags("DigitalProcesses.Forms");
        group.MapGet("/view/{id:guid}", FormDefinitionResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<FormDefinitionResponse>,FormDefinitionResponse>()
            .WithDescription("""
                Returns one FormDefinition visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", FormDefinitionResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<FormDefinitionResponse>,FormDefinitionResponse[]>()
            .WithDescription("""
                Returns FormDefinition entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", FormDefinitionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-typed", FormDefinitionResolvers.CreateApi)
            .ApplyJsonModel<DigitalProcessModelPage<FormDefinitionResponse>,FormDefinitionResponse>()
            .WithDescription("""
                Creates FormDefinition using typed creation scope for FormDefinition roots. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", FormDefinitionResolvers.CreatePage)
            .WithDescription("""
                Renders the HTML form used to create FormDefinition.
                """);
        group.MapPost("/create-form", FormDefinitionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates FormDefinition from an HTML form submission and redirects to its detail page.
                """);
    }
}
