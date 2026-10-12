using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Base.Security;
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

    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page(
        "Create process definition",
        Prefix + "/ProcessDefinitionResponse/create-form",
        Prefix + "/ProcessDefinitionResponse/list",
        "Creates a process together with its own form, workflow, and first draft versions. The request initiator uses the system role type 'já'.",
        DigitalProcessCreateForm.Text("name", "Name"),
        DigitalProcessCreateForm.TextArea("description", "Description", false));

    public static Task<ProcessDefinitionResponse> CreateApi(CreateProcessDefinitionRequest request, IProcessDefinitionService service, CancellationToken ct)
        => service.CreateTypedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IProcessDefinitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateTypedAsync(new CreateProcessDefinitionRequest(
            DigitalProcessCreateForm.Required(f, "name"),
            description: DigitalProcessCreateForm.Optional(f, "description")), ct);
        return Results.Redirect(Prefix + "/ProcessDefinitionResponse/view/" + created.id);
    }

    public static async Task<DefinitionVersionsPageModel> Detail(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        var definition = await security.GetByIdAsync<ProcessDefinitionModel>(id, SecurityOperations.Read, ct) ?? throw new KeyNotFoundException("Definition not found.");
        var versions = security.Query<ProcessDefinitionVersionModel>(SecurityOperations.Read).Where(v => v.process_definition_id == id).OrderByDescending(v => v.version)
            .Select(v => new DefinitionVersionItem(v.id, v.version, v.is_published, v.updated_at)).ToArray();
        return new DefinitionVersionsPageModel(id, definition.name, "Process", versions, ProcessDefinition: ProcessDefinitionResponse.FromModel(definition));
    }
    public static async Task<IResult> PublishVersion(Guid id, Guid definitionId, IProcessDefinitionVersionService versions, CancellationToken ct)
    {
        var published = await versions.PublishAsync(id, ct);
        if (published.process_definition_id != definitionId) return Results.BadRequest();
        return Results.Redirect($"/digital-processes/ProcessDefinitionResponse/view/{definitionId}");
    }
    public static async Task<IResult> DeleteDraft(Guid id, Guid definitionId, IProcessDefinitionVersionService versions, CancellationToken ct)
    {
        await versions.DeleteDraftAsync(id, ct);
        return Results.Redirect("/digital-processes/ProcessDefinitionResponse/view/" + definitionId);
    }
    public static async Task<IResult> CloneVersion(Guid id, IProcessDefinitionVersionService versions, CancellationToken ct)
    {
        var created = await versions.CloneLatestAsync(id, ct);
        return Results.Redirect($"/digital-processes/ProcessDefinitionResponse/view/{id}");
    }
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/ProcessDefinitionResponse").WithTags("DigitalProcesses.Process");
        group.MapActions<ProcessDefinitionModel>("ProcessDefinitionResponse");
        group.MapPost("/delete-version/{id:guid}", DeleteDraft)
            .WithDescription("Deletes an unpublished version and its dependent records if no runtime or process references exist.");
        group.MapPost("/new-version/{id:guid}", CloneVersion)
            .WithDescription("Creates a draft copy of the most recent process version, retaining form and workflow version references.");
        group.MapPost("/publish-version/{id:guid}", PublishVersion).WithDescription("Publishes one draft version through RBAC-checked services and returns to the parent definition.");
        group.MapGet("/view/{id:guid}", ProcessDefinitionResolvers.Detail)
            .ApplyViewModel<DefinitionVersionsPage,DefinitionVersionsPageModel>()
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
                Atomically creates ProcessDefinition, its owned FormDefinition and WorkflowDefinition, and all three first draft versions using typed creation scope. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", ProcessDefinitionResolvers.CreatePage)
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
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
