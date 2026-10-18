using WebAppArchitecture.DigitalProcesses.UI.ProcessDefinitionVersion.Components;
using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinition;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Services.Request;
using WebAppArchitecture.DigitalProcesses.Services.WorkflowVersion;
using WebAppArchitecture.DigitalProcesses.UI.ProcessDefinitionVersion.Pages;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.ProcessDefinitionVersion;
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
    public static DigitalProcessCreatePageModel CreatePage() => ProcessDefinitionVersionCreateForm.CreatePage();

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
        var created = await service.CreateNextAsync(new CreateProcessDefinitionVersionRequest(CreateFormValues.GuidValue(f,"process_definition_id"),CreateFormValues.GuidValue(f,"form_version_id"),CreateFormValues.GuidValue(f,"workflow_version_id"),1,CreateFormValues.BoolValue(f,"publish"),CreateFormValues.GuidValue(f,"target_group_type_id")), ct);
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
        CancellationToken ct)
    {
        var processVersion = await processVersions.GetByIdAsync<ProcessDefinitionVersionResponse>(id, ct)
            ?? throw new KeyNotFoundException("Process version not found or not readable.");
        var view = await requests.GetViewAsync(requestId, ct)
            ?? throw new KeyNotFoundException("Request not found or not readable.");
        if (view.process_definition_version_id != id)
            throw new InvalidOperationException("The test request does not belong to this process version.");
        var workspace = view.workspace ?? throw new InvalidOperationException("Request workspace was not loaded.");
        return new(processVersion, workspace.Request, workspace.Form, workspace.Workflow)
        {
            Workspace = workspace
        };
    }

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/ProcessDefinitionVersionResponse").WithTags("DigitalProcesses.Process");
        group.MapActions<ProcessDefinitionVersionModel>("ProcessDefinitionVersionResponse");
        group.MapGet("/view/{id:guid}", ProcessDefinitionVersionResolvers.Read)
            .ApplyViewModel<ProcessDefinitionVersionModelPage,ProcessDefinitionVersionResponse>()
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
            .ApplyViewModel<ProcessDefinitionVersionListPage,ProcessDefinitionVersionResponse[]>()
            .WithDescription("""
                Returns ProcessDefinitionVersion entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", ProcessDefinitionVersionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-contained", ProcessDefinitionVersionResolvers.CreateApi)
            .ApplyJsonModel<ProcessDefinitionVersionModelPage,ProcessDefinitionVersionResponse>()
            .WithDescription("""
                Creates ProcessDefinitionVersion using parent ProcessDefinition. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", ProcessDefinitionVersionResolvers.CreatePage)
            .ApplyViewModel<ProcessDefinitionVersionCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create ProcessDefinitionVersion.
                """);
        group.MapPost("/create-form", ProcessDefinitionVersionResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates ProcessDefinitionVersion from an HTML form submission and redirects to its detail page.
                """);
        group.MapPost("/publish/{id:guid}", ProcessDefinitionVersionResolvers.Publish)
            .ApplyJsonModel<ProcessDefinitionVersionModelPage,ProcessDefinitionVersionResponse>()
            .WithDescription("""
                Publishes a process-definition version after its referenced form and workflow versions are published.
                """);
    }
}
