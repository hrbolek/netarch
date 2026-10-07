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

public static class FormNodeTypeResolvers
{
    private const string Prefix = "/digital-processes";
    public static Task<IResult> Read(Guid id, HttpContext http, IFormNodeTypeService service, CancellationToken ct) => BaseResolvers.Read<FormNodeTypeModel,FormNodeTypeResponse,IFormNodeTypeService>(id,http,service,ct);
    public static Task<IResult> List(HttpContext http, IFormNodeTypeService service, CancellationToken ct) => BaseResolvers.List<FormNodeTypeModel,FormNodeTypeResponse,IFormNodeTypeService>(http,service,ct);
    public static Task<IResult> Link(Guid id, HttpContext http, IFormNodeTypeService service, CancellationToken ct) => BaseResolvers.Link<FormNodeTypeModel,FormNodeTypeResponse,IFormNodeTypeService>(id,http,service,ct);

    public static RazorComponentResult<DigitalProcessCreatePage> CreatePage() => DigitalProcessCreateForm.Page("Create form node type", Prefix+"/FormNodeTypeResponse/create-form", Prefix+"/FormNodeTypeResponse/list", "Defines form-node capabilities and the renderer key used by the Razor renderer registry.", DigitalProcessCreateForm.Text("name","Name"), DigitalProcessCreateForm.Text("renderer_key","Renderer key",help:"Examples: section, input.text, input.number, input.date, input.checkbox, action.button"), DigitalProcessCreateForm.Check("can_have_children","Can have children"), DigitalProcessCreateForm.Check("can_have_value","Can have value"), DigitalProcessCreateForm.Check("can_repeat","Can repeat"), DigitalProcessCreateForm.GuidField("parent_type_id","Parent type ID",false), DigitalProcessCreateForm.TextArea("configuration_schema_json","Configuration schema JSON",false));

    public static Task<FormNodeTypeResponse> CreateTyped(CreateFormNodeTypeRequest request, IFormNodeTypeService service, CancellationToken ct)
        => service.CreateTypedAsync(request, ct);

    public static Task<FormNodeTypeResponse> CreateContained(CreateFormNodeTypeRequest request, IFormNodeTypeService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    public static async Task<IResult> CreateForm(HttpContext http, IFormNodeTypeService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var request = new CreateFormNodeTypeRequest(DigitalProcessCreateForm.Required(f,"name"),DigitalProcessCreateForm.Required(f,"renderer_key"),DigitalProcessCreateForm.BoolValue(f,"can_have_children"),DigitalProcessCreateForm.BoolValue(f,"can_have_value"),DigitalProcessCreateForm.BoolValue(f,"can_repeat"),DigitalProcessCreateForm.OptionalGuid(f,"parent_type_id"),DigitalProcessCreateForm.Optional(f,"configuration_schema_json"));
        var created = request.parent_type_id is null
            ? await service.CreateTypedAsync(request, ct)
            : await service.CreateContainedAsync(request, ct);
        return Results.Redirect(Prefix + "/FormNodeTypeResponse/view/" + created.id);
    }

    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/FormNodeTypeResponse").WithTags("DigitalProcesses.Forms");
        group.MapGet("/view/{id:guid}", FormNodeTypeResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<FormNodeTypeResponse>,FormNodeTypeResponse>()
            .WithDescription("""
                Returns one FormNodeType visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", FormNodeTypeResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<FormNodeTypeResponse>,FormNodeTypeResponse[]>()
            .WithDescription("""
                Returns FormNodeType entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", FormNodeTypeResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-typed", FormNodeTypeResolvers.CreateTyped)
            .ApplyJsonModel<DigitalProcessModelPage<FormNodeTypeResponse>,FormNodeTypeResponse>()
            .WithDescription("""
                Creates a root FormNodeType under the system FormNodeType creation scope. The client cannot choose an RBAC target.
                """);
        group.MapPost("/create-contained", FormNodeTypeResolvers.CreateContained)
            .ApplyJsonModel<DigitalProcessModelPage<FormNodeTypeResponse>,FormNodeTypeResponse>()
            .WithDescription("""
                Creates a child FormNodeType under parent_type_id. Create permission is evaluated on the parent type's RBAC scope.
                """);

        group.MapGet("/create", FormNodeTypeResolvers.CreatePage)
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
