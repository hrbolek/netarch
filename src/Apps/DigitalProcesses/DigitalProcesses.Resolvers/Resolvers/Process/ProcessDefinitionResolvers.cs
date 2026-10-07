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

public static class ProcessDefinitionResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IProcessDefinitionService service, CancellationToken ct) => BaseResolvers.Read<ProcessDefinitionModel,ProcessDefinitionResponse,IProcessDefinitionService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IProcessDefinitionService service, CancellationToken ct) => BaseResolvers.List<ProcessDefinitionModel,ProcessDefinitionResponse,IProcessDefinitionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IProcessDefinitionService service, CancellationToken ct) => BaseResolvers.Link<ProcessDefinitionModel,ProcessDefinitionResponse,IProcessDefinitionService>(id,http,service,ct);

    public static RazorComponentResult<DigitalProcessCreatePage> CreatePage() => DigitalProcessCreateForm.Page("Create process definition", Prefix+"/ProcessDefinitionResponse/create-form", Prefix+"/ProcessDefinitionResponse/list", "Creates a digital-process definition and selects the role type assigned to request initiators.", DigitalProcessCreateForm.Text("name","Name"),DigitalProcessCreateForm.GuidField("initiator_role_type_id","Initiator role type ID"),DigitalProcessCreateForm.TextArea("description","Description",false));

    public static Task<ProcessDefinitionResponse> CreateApi(CreateProcessDefinitionRequest request, IProcessDefinitionService service, CancellationToken ct)
        => service.CreateTypedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IProcessDefinitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateTypedAsync(new CreateProcessDefinitionRequest(DigitalProcessCreateForm.Required(f,"name"),DigitalProcessCreateForm.GuidValue(f,"initiator_role_type_id"),DigitalProcessCreateForm.Optional(f,"description")), ct);
        return Results.Redirect(Prefix + "/ProcessDefinitionResponse/view/" + created.id);
    }

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/ProcessDefinitionResponse").WithTags("DigitalProcesses.Process");
        group.MapGet("/view/{id:guid}", ProcessDefinitionResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<ProcessDefinitionResponse>,ProcessDefinitionResponse>()
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
                Creates ProcessDefinition using typed creation scope for ProcessDefinition roots. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", ProcessDefinitionResolvers.CreatePage)
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
