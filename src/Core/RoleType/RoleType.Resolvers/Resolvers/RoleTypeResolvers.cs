using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.RoleType.Models;
using WebAppArchitecture.RoleType.Services;
using WebAppArchitecture.RoleType.UI.Pages;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.RoleType.Resolvers;

/// <summary>
/// HTTP resolver pro Role Type Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class RoleTypeResolvers
{
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(id, httpContext, service, cancellationToken);

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.List<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(httpContext, service, cancellationToken);

    /// <summary>
    /// Zpracovává změnu existující entity; ověření oprávnění a integrity musí probíhat ve službě, ne pouze v UI. Volání: Update; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static async Task<IResult> Update(RoleTypeUpdateRequest request, HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
    {
        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actorUserId = actorProvider.TryGetActorUserId(httpContext);
        if (actorUserId is null)
            return Results.Unauthorized();

        var stampedRequest = request with
        {
            updated_at = DateTimeOffset.UtcNow,
            updated_by = actorUserId
        };

        return await BaseResolvers.Update<RoleTypeModel, RoleTypeResponse, RoleTypeUpdateRequest, IRoleTypeService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> UpdateForm(
        Guid id,
        HttpContext httpContext,
        IRoleTypeService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var name = form["name"].ToString();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "name is required." });

        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actorUserId = actorProvider.TryGetActorUserId(httpContext);
        if (actorUserId is null)
            return Results.Unauthorized();

        var existing = await service.GetByIdAsync<RoleTypeResponse>(id, cancellationToken);
        if (existing is null)
            return Results.NotFound();

        var authorization = await BaseResolvers.AuthorizeModelAsync(
            httpContext,
            existing,
            RbacOperations.Update,
            cancellationToken);

        if (authorization is not null)
            return authorization;

        var request = new RoleTypeUpdateRequest(
            id,
            name,
            existing.operations,
            DateTimeOffset.UtcNow,
            actorUserId);

        var updated = await service.UpdateAsync<RoleTypeUpdateRequest, RoleTypeResponse>(request, cancellationToken);
        return ApiResults.Ok(updated);
    }


    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IRoleTypeService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(
            id, httpContext, service, cancellationToken);

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: Delete; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(id, httpContext, service, cancellationToken);
}
