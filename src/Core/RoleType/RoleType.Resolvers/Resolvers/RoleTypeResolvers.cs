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

public static class RoleTypeResolvers
{
    public static Task<IResult> Read(Guid id, HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(id, httpContext, service, cancellationToken);

    public static Task<IResult> List(HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.List<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(httpContext, service, cancellationToken);

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

        var existing = await service.GetByIdAsync(id, cancellationToken);
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

        var updated = await service.UpdateAsync(request, cancellationToken);
        return ApiResults.Ok(updated);
    }


    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IRoleTypeService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(
            id, httpContext, service, cancellationToken);

    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(id, httpContext, service, cancellationToken);
}

public static class RoleTypeEndpointExtensions
{
    public static IEndpointRouteBuilder MapRoleTypeEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/RoleTypeResponse").WithTags("RoleType");

        group.MapGet("", RedirectResolvers.List)
            .WithDescription("Redirects the short entity collection URI to the canonical /list endpoint while preserving query parameters.");

        group.MapGet("/{id:guid}", RedirectResolvers.View)
            .WithDescription("Redirects the short entity URI to the canonical /view/{id} endpoint while preserving query parameters.");


        group.MapGet("/link/{id:guid}", RoleTypeResolvers.Link)
            .WithName("RoleTypeLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>()
                .WithDescription("""
                    GET /link/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/view/{id:guid}", RoleTypeResolvers.Read)
            .WithName("RoleTypeRead")
            .ApplyViewModel<RoleTypeViewPage, RoleTypeResponse>()
                .WithDescription("""
                    GET /view/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/edit/{id:guid}", RoleTypeResolvers.Read)
            .WithName("RoleTypeEdit")
            .ApplyViewModel<RoleTypeEditPage, RoleTypeResponse>()
                .WithDescription("""
                    GET /edit/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/list", RoleTypeResolvers.List)
            .WithName("RoleTypeList")
            .ApplyViewModel<RoleTypeViewListPage, RoleTypeResponse[]>()
                .WithDescription("""
                    Lists secured role types. Optional `where` accepts a JSON Hasura-like filter expression
                    that is translated to an IQueryable predicate before database materialization. Optional `take`
                    limits the result to 1..200 rows and is intended for selectors and incremental lookup UI.
                    """);

        group.MapPut("/update", RoleTypeResolvers.Update)
            .WithName("RoleTypeUpdate")
            .ApplyJsonModel<RoleTypeResponse>()
                .WithDescription("""
                    PUT /update. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/update/{id:guid}", RoleTypeResolvers.UpdateForm)
            .WithName("RoleTypeUpdateForm")
            .ApplyViewModel<RoleTypeEditPage, RoleTypeResponse>()
                .WithDescription("""
                    POST /update/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapDelete("/delete", RoleTypeResolvers.Delete)
            .WithName("RoleTypeDelete")
                .WithDescription("""
                    DELETE /delete. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        return endpoints;
    }
}
