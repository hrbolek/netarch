using WebAppArchitecture.DigitalProcesses.DBLayer;
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
using WebAppArchitecture.DigitalProcesses.UI.Designer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

/// <summary>
/// HTTP resolver pro Process Definition Version Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class ProcessDefinitionVersionResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IProcessDefinitionVersionService service, CancellationToken ct) => BaseResolvers.Read<ProcessDefinitionVersionModel,ProcessDefinitionVersionResponse,IProcessDefinitionVersionService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IProcessDefinitionVersionService service, CancellationToken ct) => BaseResolvers.List<ProcessDefinitionVersionModel,ProcessDefinitionVersionResponse,IProcessDefinitionVersionService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateContainedAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IProcessDefinitionVersionService service, CancellationToken ct) => BaseResolvers.Link<ProcessDefinitionVersionModel,ProcessDefinitionVersionResponse,IProcessDefinitionVersionService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage() => DigitalProcessCreateForm.Page(
        "Create process definition version", Prefix + "/ProcessDefinitionVersionResponse/create-form",
        Prefix + "/ProcessDefinitionVersionResponse/list",
        "Choose a process, a form and workflow, then their published versions.",
        DigitalProcessCreateForm.EntityLookup("process_definition_id", "Process", Prefix + "/ProcessDefinitionResponse/list", ["name"]),
        DigitalProcessCreateForm.EntityLookup("form_definition_id", "Form", Prefix + "/FormDefinitionResponse/list", ["name"]),
        DigitalProcessCreateForm.EntityLookup("form_version_id", "Published form version", Prefix + "/FormVersionResponse/list?where=%7B%22is_published%22%3A%7B%22_eq%22%3Atrue%7D%7D", [], parentField: "form_definition_id", parentProperty: "form_definition_id"),
        DigitalProcessCreateForm.EntityLookup("workflow_definition_id", "Workflow", Prefix + "/WorkflowDefinitionResponse/list", ["name"]),
        DigitalProcessCreateForm.EntityLookup("workflow_version_id", "Published workflow version", Prefix + "/WorkflowVersionResponse/list?where=%7B%22is_published%22%3A%7B%22_eq%22%3Atrue%7D%7D", [], parentField: "workflow_definition_id", parentProperty: "workflow_definition_id"),
        DigitalProcessCreateForm.EntityLookup("target_group_type_id", "Allowed target group type", "/architecture/GroupTypeResponse/list", ["name"]),
        DigitalProcessCreateForm.Check("publish", "Publish immediately"));

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, PublishAsync, Query, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<ProcessDefinitionVersionResponse> CreateApi(CreateProcessDefinitionVersionRequest request, IProcessDefinitionVersionService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, PublishAsync, Query, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IProcessDefinitionVersionService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateNextAsync(new CreateProcessDefinitionVersionRequest(DigitalProcessCreateForm.GuidValue(f,"process_definition_id"),DigitalProcessCreateForm.GuidValue(f,"form_version_id"),DigitalProcessCreateForm.GuidValue(f,"workflow_version_id"),1,DigitalProcessCreateForm.BoolValue(f,"publish"),DigitalProcessCreateForm.GuidValue(f,"target_group_type_id")), ct);
        return Results.Redirect(Prefix + "/ProcessDefinitionResponse/view/" + created.process_definition_id);
    }
    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: PublishAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<ProcessDefinitionVersionResponse> Publish(Guid id, IProcessDefinitionVersionService service, CancellationToken ct) => service.PublishAsync(id, ct);

    public static async Task<IResult> CreateTestRequest(Guid id, IRequestService service, CancellationToken ct)
    {
        var request = await service.CreateTestRequestForProcessVersionAsync(id, ct);
        return Results.Redirect($"{Prefix}/ProcessDefinitionVersionResponse/test/{id}?requestId={request.id}");
    }

    public static async Task<ProcessVersionTestPageModel> TestPage(
        Guid id,
        Guid requestId,
        IProcessDefinitionVersionService processVersions,
        IRequestService requests,
        IFormNodeService forms,
        IWorkflowVersionService workflows,
        CancellationToken ct)
    {
        var processVersion = await processVersions.GetByIdAsync<ProcessDefinitionVersionResponse>(id, ct)
            ?? throw new KeyNotFoundException("Process version not found or not readable.");
        var request = await requests.GetDetailAsync(requestId, ct);
        if (request.request.process_definition_version_id != id)
            throw new InvalidOperationException("The test request does not belong to this process version.");
        var form = await forms.GetDesignerAsync(processVersion.form_version_id, ct);
        var workflow = await workflows.GetDesignerAsync(processVersion.workflow_version_id, ct);
        return new(processVersion, request, form, workflow);
    }

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/ProcessDefinitionVersionResponse").WithTags("DigitalProcesses.Process");
        group.MapActions<ProcessDefinitionVersionModel>("ProcessDefinitionVersionResponse");
        group.MapGet("/view/{id:guid}", ProcessDefinitionVersionResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<ProcessDefinitionVersionResponse>,ProcessDefinitionVersionResponse>()
            .WithDescription("""
                Returns one ProcessDefinitionVersion visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/test/{id:guid}", ProcessDefinitionVersionResolvers.TestPage)
            .ApplyViewModel<ProcessVersionTestPage,ProcessVersionTestPageModel>()
            .WithDescription("Opens a test Request for this exact process version, including its editable form tree, current workflow transitions and history.");
        group.MapPost("/test/{id:guid}", ProcessDefinitionVersionResolvers.CreateTestRequest)
            .DisableAntiforgery()
            .WithDescription("Creates a test Request using this exact process version, including drafts, then redirects to its detail and transition history.");
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
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
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
