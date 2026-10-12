using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.UI.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

/// <summary>
/// Vstupní kontrakt Request Transition History Resolvers. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public static class RequestTransitionHistoryResolvers
{
    private const string Prefix = "/digital-processes";
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List, Link; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext http, IRequestTransitionHistoryService service, CancellationToken ct) => BaseResolvers.Read<RequestTransitionHistoryModel,RequestTransitionHistoryResponse,IRequestTransitionHistoryService>(id,http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List, Link; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext http, IRequestTransitionHistoryService service, CancellationToken ct) => BaseResolvers.List<RequestTransitionHistoryModel,RequestTransitionHistoryResponse,IRequestTransitionHistoryService>(http,service,ct);
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Link; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Link(Guid id, HttpContext http, IRequestTransitionHistoryService service, CancellationToken ct) => BaseResolvers.Link<RequestTransitionHistoryModel,RequestTransitionHistoryResponse,IRequestTransitionHistoryService>(id,http,service,ct);

    /// <summary>
    /// Registruje routy Minimal API a vazbu endpointů na Razor/JSON reprezentaci; samotné požadavky zpracovávají příslušné resolver metody.
    /// </summary>
    public static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/RequestTransitionHistoryResponse").WithTags("DigitalProcesses.Runtime");
        group.MapActions<RequestTransitionHistoryModel>("RequestTransitionHistoryResponse");
        group.MapGet("/view/{id:guid}", RequestTransitionHistoryResolvers.Read)
            .ApplyViewModel<DigitalProcessModelPage<RequestTransitionHistoryResponse>,RequestTransitionHistoryResponse>()
            .WithDescription("""
                Returns one RequestTransitionHistory visible to the current actor through the standard RBAC read operation. HTML or JSON is selected by content negotiation.
                """);
        group.MapGet("/list", RequestTransitionHistoryResolvers.List)
            .ApplyViewModel<DigitalProcessListPage<RequestTransitionHistoryResponse>,RequestTransitionHistoryResponse[]>()
            .WithDescription("""
                Returns RequestTransitionHistory entities visible to the current actor. Optional Hasura-style where filtering is applied to IQueryable before SQL execution.
                """);
        group.MapGet("/link/{id:guid}", RequestTransitionHistoryResolvers.Link)
            .ApplyViewModel<EntityLinkFragment,EntityLinkResponse>()
            .WithDescription("""
                Returns the entity reference used by lazy EntityLink rendering or JSON clients.
                """);
    }
}
