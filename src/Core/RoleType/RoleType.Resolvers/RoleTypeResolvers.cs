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

    public static async Task<IResult> UpdateForm(
        Guid id,
        HttpContext httpContext,
        IRoleTypeService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        if (!BaseResolvers.TryReadUpdateToken(form, out var expectedUpdatedAt))
            return Results.BadRequest(new { error = "A valid updated_at token is required." });
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
            expectedUpdatedAt,
            name,
            existing.operations);

        var updated = await service.UpdateAsync<RoleTypeUpdateRequest, RoleTypeResponse>(request, cancellationToken);
        return ApiResults.Ok(updated);
    }

}
