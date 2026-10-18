using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using WebAppArchitecture.DigitalProcesses.UI.Shared.Components;
using WebAppArchitecture.DigitalProcesses.UI.Submission.Pages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.Submission;
/// <summary>
/// HTTP resolver pro Submission Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>

public static class SubmissionResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link, GetTreeAsync, UpdateSubmissionValuesAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, ISubmissionService service, CancellationToken ct) => BaseResolvers.Read<SubmissionModel,SubmissionResponse,ISubmissionService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link, GetTreeAsync, UpdateSubmissionValuesAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, ISubmissionService service, CancellationToken ct) => BaseResolvers.List<SubmissionModel,SubmissionResponse,ISubmissionService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link, GetTreeAsync, UpdateSubmissionValuesAsync; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, ISubmissionService service, CancellationToken ct) => BaseResolvers.Link<SubmissionModel,SubmissionResponse,ISubmissionService>(id,http,service,ct);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: GetTreeAsync, UpdateSubmissionValuesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<SubmissionTreeResponse> Tree(Guid id, ISubmissionService service, CancellationToken ct) => service.GetTreeAsync(id, ct);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: UpdateSubmissionValuesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public static Task<SubmissionTreeResponse> BulkValues(Guid id, BulkSubmissionValuesRequest request, ISubmissionService service, CancellationToken ct)
        => service.UpdateValuesAsync(id, request, ct);

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/SubmissionResponse").WithTags("DigitalProcesses.Runtime");
        group.MapActions<SubmissionModel>("SubmissionResponse");
        group.MapGet("/view/{id:guid}", SubmissionResolvers.Read)
            .ApplyViewModel<SubmissionModelPage,SubmissionResponse>()
            .WithDescription("""
                Returns one Submission visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", SubmissionResolvers.List)
            .ApplyViewModel<SubmissionListPage,SubmissionResponse[]>()
            .WithDescription("""
                Returns Submission entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", SubmissionResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);
        group.MapPut("/tree/{id:guid}", SubmissionResolvers.BulkValues)
            .WithDescription("Atomically update supplied submission-node values; omitted nodes retain their data. Deny and RBAC Update are enforced per node; archived submissions cannot be edited.");
        group.MapGet("/tree/{id:guid}", SubmissionResolvers.Tree)
            .ApplyViewModel<SubmissionTreePage,SubmissionTreeResponse>()
            .WithDescription("""
                Returns the authorized submission tree. Nodes without read permission are omitted before rendering or JSON serialization.
                """);
    }
}
