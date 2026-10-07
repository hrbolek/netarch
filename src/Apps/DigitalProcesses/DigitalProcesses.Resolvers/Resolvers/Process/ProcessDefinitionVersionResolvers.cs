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

public static class ProcessDefinitionVersionResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IProcessDefinitionVersionService service, CancellationToken ct) => BaseResolvers.Read<ProcessDefinitionVersionModel,ProcessDefinitionVersionResponse,IProcessDefinitionVersionService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IProcessDefinitionVersionService service, CancellationToken ct) => BaseResolvers.List<ProcessDefinitionVersionModel,ProcessDefinitionVersionResponse,IProcessDefinitionVersionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IProcessDefinitionVersionService service, CancellationToken ct) => BaseResolvers.Link<ProcessDefinitionVersionModel,ProcessDefinitionVersionResponse,IProcessDefinitionVersionService>(id,http,service,ct);

    public static RazorComponentResult<DigitalProcessCreatePage> CreatePage() => DigitalProcessCreateForm.Page("Create process definition version", Prefix+"/ProcessDefinitionVersionResponse/create-form", Prefix+"/ProcessDefinitionVersionResponse/list", "Connects one form version with one workflow version in a versioned process definition.", DigitalProcessCreateForm.GuidField("process_definition_id","Process definition ID"),DigitalProcessCreateForm.GuidField("form_version_id","Form version ID"),DigitalProcessCreateForm.GuidField("workflow_version_id","Workflow version ID"),DigitalProcessCreateForm.Number("version","Version","1","1"),DigitalProcessCreateForm.Check("publish","Publish immediately"));

    public static Task<ProcessDefinitionVersionResponse> CreateApi(CreateProcessDefinitionVersionRequest request, IProcessDefinitionVersionService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IProcessDefinitionVersionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateProcessDefinitionVersionRequest(DigitalProcessCreateForm.GuidValue(f,"process_definition_id"),DigitalProcessCreateForm.GuidValue(f,"form_version_id"),DigitalProcessCreateForm.GuidValue(f,"workflow_version_id"),DigitalProcessCreateForm.IntValue(f,"version"),DigitalProcessCreateForm.BoolValue(f,"publish")), ct);
        return Results.Redirect(Prefix + "/ProcessDefinitionVersionResponse/view/" + created.id);
    }
    public static Task<ProcessDefinitionVersionResponse> Publish(Guid id, IProcessDefinitionVersionService service, CancellationToken ct) => service.PublishAsync(id, ct);

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/ProcessDefinitionVersionResponse").WithTags("DigitalProcesses.Process");
        group.MapGet("/view/{id:guid}", ProcessDefinitionVersionResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<ProcessDefinitionVersionResponse>,ProcessDefinitionVersionResponse>()
            .WithDescription("""
                Returns one ProcessDefinitionVersion visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", ProcessDefinitionVersionResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<ProcessDefinitionVersionResponse>,ProcessDefinitionVersionResponse[]>()
            .WithDescription("""
                Returns ProcessDefinitionVersion entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", ProcessDefinitionVersionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-contained", ProcessDefinitionVersionResolvers.CreateApi)
            .ApplyJsonModel<DigitalProcessModelPage<ProcessDefinitionVersionResponse>,ProcessDefinitionVersionResponse>()
            .WithDescription("""
                Creates ProcessDefinitionVersion using parent ProcessDefinition. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", ProcessDefinitionVersionResolvers.CreatePage)
            .WithDescription("""
                Renders the HTML form used to create ProcessDefinitionVersion.
                """);
        group.MapPost("/create-form", ProcessDefinitionVersionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates ProcessDefinitionVersion from an HTML form submission and redirects to its detail page.
                """);
        group.MapPost("/publish/{id:guid}", ProcessDefinitionVersionResolvers.Publish)
            .ApplyJsonModel<DigitalProcessModelPage<ProcessDefinitionVersionResponse>,ProcessDefinitionVersionResponse>()
            .WithDescription("""
                Publishes a process-definition version after its referenced form and workflow versions are published.
                """);
    }
}
