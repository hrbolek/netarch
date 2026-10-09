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
    public static Task<FormDefinitionDetailResponse> Read(Guid id, IFormDefinitionService service, CancellationToken ct)
        => service.GetDetailAsync(id, ct);
    public static Task<IResult> List(HttpContext http, IFormDefinitionService service, CancellationToken ct) => BaseResolvers.List<FormDefinitionModel,FormDefinitionResponse,IFormDefinitionService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IFormDefinitionService service, CancellationToken ct) => BaseResolvers.Link<FormDefinitionModel,FormDefinitionResponse,IFormDefinitionService>(id,http,service,ct);

    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page("Create form definition", Prefix+"/FormDefinitionResponse/create-form", Prefix+"/FormDefinitionResponse/list", "Creates the stable identity of a form together with draft version 1. Form structure is edited in that draft version.", DigitalProcessCreateForm.Text("name","Name"), DigitalProcessCreateForm.TextArea("description","Description",false));

    public static Task<FormDefinitionResponse> CreateApi(CreateFormDefinitionRequest request, IFormDefinitionService service, CancellationToken ct)
        => service.CreateTypedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IFormDefinitionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateTypedAsync(new CreateFormDefinitionRequest(DigitalProcessCreateForm.Required(f,"name"),DigitalProcessCreateForm.Optional(f,"description")), ct);
        return Results.Redirect(Prefix + "/FormDefinitionResponse/view/" + created.id);
    }

    public static async Task<IResult> NewVersion(Guid id, IFormDefinitionService definitions, IFormVersionService versions, WebAppArchitecture.SecurityLayer.ISecurityLayer security, CancellationToken ct)
    {
        _ = await definitions.GetDetailAsync(id, ct);
        var next = security.Query<FormVersionModel>(WebAppArchitecture.SecurityLayer.SecurityOperations.Read).Where(v => v.form_definition_id == id).Select(v => v.version).ToArray().DefaultIfEmpty(0).Max() + 1;
        var created = await versions.CreateContainedAsync(new CreateFormVersionRequest(id, next), ct);
        return Results.Redirect($"/digital-processes/FormVersionResponse/designer/{created.id}");
    }
    public static async Task<IResult> PublishVersion(Guid id, Guid definitionId, IFormVersionService versions, CancellationToken ct)
    {
        var published = await versions.PublishAsync(id, ct);
        if (published.form_definition_id != definitionId) return Results.BadRequest();
        return Results.Redirect($"/digital-processes/FormDefinitionResponse/view/{definitionId}");
    }
    public static async Task<IResult> DeleteDraft(Guid id, Guid definitionId, IFormVersionService versions, CancellationToken ct)
    {
        await versions.DeleteDraftAsync(id, ct);
        return Results.Redirect("/digital-processes/FormDefinitionResponse/view/" + definitionId);
    }
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormDefinitionResponse").WithTags("DigitalProcesses.Forms");
        group.MapActions<FormDefinitionModel>("FormDefinitionResponse");
        group.MapPost("/delete-version/{id:guid}", DeleteDraft)
            .WithDescription("Deletes an unpublished version and its dependent records if no runtime or process references exist.");
        group.MapPost("/new-version/{id:guid}", NewVersion).WithDescription("Creates the next draft form version from its parent definition, with server-assigned version number and RBAC checks.");
        group.MapPost("/publish-version/{id:guid}", PublishVersion).WithDescription("Publishes a draft form version and returns to the owning form definition.");
        group.MapGet("/view/{id:guid}", FormDefinitionResolvers.Read)
            .ApplyViewModel<FormDefinitionDetailPage,FormDefinitionDetailResponse>()
            .WithDescription("""
                Returns one FormDefinition together with all readable versions. The HTML view exposes draft Designer/Test actions.
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
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
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
