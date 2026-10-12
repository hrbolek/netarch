using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.SecurityLayer;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.UI.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

/// <summary>
/// HTTP resolver pro Workflow State Access Rule Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class WorkflowStateAccessRuleResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IWorkflowStateAccessRuleService service, CancellationToken ct) => BaseResolvers.Read<WorkflowStateAccessRuleModel,WorkflowStateAccessRuleResponse,IWorkflowStateAccessRuleService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateContainedAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IWorkflowStateAccessRuleService service, CancellationToken ct) => BaseResolvers.List<WorkflowStateAccessRuleModel,WorkflowStateAccessRuleResponse,IWorkflowStateAccessRuleService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateContainedAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IWorkflowStateAccessRuleService service, CancellationToken ct) => BaseResolvers.Link<WorkflowStateAccessRuleModel,WorkflowStateAccessRuleResponse,IWorkflowStateAccessRuleService>(id,http,service,ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static DigitalProcessCreatePageModel CreatePage(ISecurityLayer security)
    {
        return DigitalProcessCreateForm.Page("Create workflow state access rule", Prefix+"/WorkflowStateAccessRuleResponse/create-form", Prefix+"/WorkflowStateAccessRuleResponse/list",
            "Defines how source roles are compiled into RBAC roles for a request, submission or form-node scope.",
            DigitalProcessCreateForm.EntityLookup("workflow_state_id", "Workflow state", Prefix+"/WorkflowStateResponse/list", ["name", "key"]),
            DigitalProcessCreateForm.EntityLookup("source_role_type_id", "Source role type", "/architecture/RoleTypeResponse/list", ["name"]),
            DigitalProcessCreateForm.EntityLookup("target_role_type_id", "Target role type", "/architecture/RoleTypeResponse/list", ["name"]),
            DigitalProcessCreateForm.Text("target_scope", "Target scope", help:"request, submission, or form-node"),
            DigitalProcessCreateForm.EntityLookup("target_form_node_id", "Target form node", Prefix+"/FormNodeResponse/list", ["key", "label"], false));
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<WorkflowStateAccessRuleResponse> CreateApi(CreateWorkflowStateAccessRuleRequest request, IWorkflowStateAccessRuleService service, CancellationToken ct)
        => service.CreateContainedAsync(request, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateContainedAsync, ReadFormAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> CreateForm(HttpContext http, IWorkflowStateAccessRuleService service, CancellationToken ct)
    {
        var f = await http.Request.ReadFormAsync(ct);
        var created = await service.CreateContainedAsync(new CreateWorkflowStateAccessRuleRequest(DigitalProcessCreateForm.GuidValue(f,"workflow_state_id"),DigitalProcessCreateForm.GuidValue(f,"source_role_type_id"),DigitalProcessCreateForm.GuidValue(f,"target_role_type_id"),DigitalProcessCreateForm.Required(f,"target_scope"),DigitalProcessCreateForm.OptionalGuid(f,"target_form_node_id")), ct);
        return Results.Redirect(Prefix + "/WorkflowStateAccessRuleResponse/view/" + created.id);
    }

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/WorkflowStateAccessRuleResponse").WithTags("DigitalProcesses.Workflow");
        group.MapActions<WorkflowStateAccessRuleModel>("WorkflowStateAccessRuleResponse");
        group.MapGet("/view/{id:guid}", WorkflowStateAccessRuleResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<WorkflowStateAccessRuleResponse>,WorkflowStateAccessRuleResponse>()
            .WithDescription("""
                Returns one WorkflowStateAccessRule visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", WorkflowStateAccessRuleResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<WorkflowStateAccessRuleResponse>,WorkflowStateAccessRuleResponse[]>()
            .WithDescription("""
                Returns WorkflowStateAccessRule entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", WorkflowStateAccessRuleResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);

        group.MapPost("/create-contained", WorkflowStateAccessRuleResolvers.CreateApi)
            .ApplyJsonModel<DigitalProcessModelPage<WorkflowStateAccessRuleResponse>,WorkflowStateAccessRuleResponse>()
            .WithDescription("""
                Creates WorkflowStateAccessRule using parent WorkflowState. The authorization target is derived server-side; clients cannot supply rbacobject_id.
                """);

        group.MapGet("/create", WorkflowStateAccessRuleResolvers.CreatePage)
            .ApplyViewModel<DigitalProcessCreatePage, DigitalProcessCreatePageModel>()
            .WithDescription("""
                Renders the HTML form used to create WorkflowStateAccessRule.
                """);
        group.MapPost("/create-form", WorkflowStateAccessRuleResolvers.CreateForm)
            .DisableAntiforgery()
            .WithDescription("""
                Creates WorkflowStateAccessRule from an HTML form submission and redirects to its detail page.
                """);
    }
}
