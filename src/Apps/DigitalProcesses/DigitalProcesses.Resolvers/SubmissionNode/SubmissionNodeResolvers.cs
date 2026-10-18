using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.UI.SubmissionNode.Pages;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.SubmissionNode;
/// <summary>
/// HTTP resolver pro Submission Node Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>

public static class SubmissionNodeResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, CreateInSubmissionAsync, CreateInNodeAsync, SetValueAsync, GetSubtreeAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, ISubmissionNodeService service, CancellationToken ct) => BaseResolvers.Read<SubmissionNodeModel,SubmissionNodeResponse,ISubmissionNodeService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, CreateInSubmissionAsync, CreateInNodeAsync, SetValueAsync, GetSubtreeAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, ISubmissionNodeService service, CancellationToken ct) => BaseResolvers.List<SubmissionNodeModel,SubmissionNodeResponse,ISubmissionNodeService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, CreateInSubmissionAsync, CreateInNodeAsync, SetValueAsync, GetSubtreeAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, ISubmissionNodeService service, CancellationToken ct) => BaseResolvers.Link<SubmissionNodeModel,SubmissionNodeResponse,ISubmissionNodeService>(id,http,service,ct);
    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateInSubmissionAsync, CreateInNodeAsync, SetValueAsync, GetSubtreeAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<SubmissionNodeResponse> CreateInSubmission(CreateSubmissionNodeRequest request, ISubmissionNodeService service, CancellationToken ct)
        => service.CreateInSubmissionAsync(request, ct);
    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CreateInNodeAsync, SetValueAsync, GetSubtreeAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<SubmissionNodeResponse> CreateInNode(CreateSubmissionNodeRequest request, ISubmissionNodeService service, CancellationToken ct)
        => service.CreateInNodeAsync(request, ct);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: SetValueAsync, GetSubtreeAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static async Task<IResult> SetValue(Guid id, SetSubmissionNodeValueRequest request, ISubmissionNodeService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.SetValueAsync(id, request, ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
    }
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: GetSubtreeAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static async Task<SubmissionNodeResponse[]> Subtree(Guid id, ISubmissionNodeService service, CancellationToken ct)
        => (await service.GetSubtreeAsync(id, ct)).ToArray();

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/SubmissionNodeResponse").WithTags("DigitalProcesses.Runtime");
        group.MapActions<SubmissionNodeModel>("SubmissionNodeResponse");
        group.MapGet("/view/{id:guid}", SubmissionNodeResolvers.Read)
            .ApplyViewModel<SubmissionNodeModelPage,SubmissionNodeResponse>()
            .WithDescription("""
                Returns one SubmissionNode visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", SubmissionNodeResolvers.List)
            .ApplyViewModel<SubmissionNodeListPage,SubmissionNodeResponse[]>()
            .WithDescription("""
                Returns SubmissionNode entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", SubmissionNodeResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);
        group.MapPost("/create-in-submission", SubmissionNodeResolvers.CreateInSubmission)
            .ApplyJsonModel<SubmissionNodeModelPage,SubmissionNodeResponse>()
            .WithDescription("""
                Creates a root runtime SubmissionNode inside submission_id. Create permission is evaluated on the existing Submission RBAC scope. The form definition must identify a root FormNode; rbacobject_id cannot be supplied by the client.
                """);
        group.MapPost("/create-in-node", SubmissionNodeResolvers.CreateInNode)
            .ApplyJsonModel<SubmissionNodeModelPage,SubmissionNodeResponse>()
            .WithDescription("""
                Creates a child runtime SubmissionNode below parent_id. Create permission is evaluated on the parent SubmissionNode RBAC scope. The service enforces submission ownership, form-tree structure and occurrence limits; rbacobject_id cannot be supplied by the client.
                """);
        group.MapPut("/value/{id:guid}", SubmissionNodeResolvers.SetValue)
            .WithDescription("""
                Stores a value in an editable submission node; archived submissions remain immutable.
                """);
        group.MapGet("/subtree/{id:guid}", SubmissionNodeResolvers.Subtree)
            .ApplyJsonModel<SubmissionNodeListPage,SubmissionNodeResponse[]>()
            .WithDescription("""
                Returns the authorized runtime subtree rooted at the selected submission node.
                """);
    }
}
