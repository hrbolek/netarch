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
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Membership.UI.Pages;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Resolvers;

public static class MembershipResolvers
{
    public static async Task<IResult> CreatePage(
        ICurrentActorProvider actorProvider,
        IRbacAuthorizationService authorizationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null) return Results.Unauthorized();
        if (Guid.TryParse(httpContext.Request.Query["group_id"], out var groupId) &&
            !await authorizationService.IsAllowedAsync(actor.Value, groupId, RbacOperations.Create, cancellationToken))
            return Results.Forbid();
        return new RazorComponentResult<MembershipCreatePage>();
    }

    public static async Task<IResult> CreateContained(
        MembershipCreateContainedRequest request,
        IMembershipService service,
        CancellationToken cancellationToken)
        => ApiResults.Ok(await service.CreateContainedAsync(request, cancellationToken));

    public static async Task<IResult> CreateForm(
        HttpContext httpContext,
        IMembershipService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        if (!Guid.TryParse(form["group_id"].ToString(), out var groupId))
            return Results.BadRequest(new { error = "Invalid group_id value." });
        if (!Guid.TryParse(form["user_id"].ToString(), out var userId))
            return Results.BadRequest(new { error = "Invalid user_id value." });
        var validFrom = DateTimeOffset.UtcNow;
        var rawFrom = form["valid_from"].ToString();
        if (!string.IsNullOrWhiteSpace(rawFrom) && !DateTimeOffset.TryParse(rawFrom, out validFrom))
            return Results.BadRequest(new { error = "Invalid valid_from value." });
        DateTimeOffset? validTo = null;
        var rawTo = form["valid_to"].ToString();
        if (!string.IsNullOrWhiteSpace(rawTo))
        {
            if (!DateTimeOffset.TryParse(rawTo, out var parsed))
                return Results.BadRequest(new { error = "Invalid valid_to value." });
            validTo = parsed;
        }
        var created = await service.CreateContainedAsync(
            new MembershipCreateContainedRequest(groupId, userId, validFrom, validTo), cancellationToken);
        var returnUrl = form["return_url"].ToString();
        return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl)
            ? $"/architecture/MembershipResponse/edit/{created.id}"
            : returnUrl);
    }

    public static Task<IResult> Read(Guid id, HttpContext httpContext, IMembershipService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<MembershipModel, MembershipResponse, IMembershipService>(id, httpContext, service, cancellationToken);

    public static Task<IResult> List(HttpContext httpContext, IMembershipService service, CancellationToken cancellationToken)
        => BaseResolvers.List<MembershipModel, MembershipResponse, IMembershipService>(httpContext, service, cancellationToken);

    public static async Task<IResult> Update(MembershipUpdateRequest request, HttpContext httpContext, IMembershipService service, CancellationToken cancellationToken)
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

        return await BaseResolvers.Update<MembershipModel, MembershipResponse, MembershipUpdateRequest, IMembershipService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> UpdateForm(
        Guid id,
        HttpContext httpContext,
        IMembershipService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var validFromText = form["valid_from"].ToString();
        var validToText = form["valid_to"].ToString();

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

        if (!DateTimeOffset.TryParse(validFromText, out var parsedValidFrom))
            return Results.BadRequest(new { error = "Invalid valid_from value." });

        DateTimeOffset? parsedValidTo = null;
        if (!string.IsNullOrWhiteSpace(validToText))
        {
            if (!DateTimeOffset.TryParse(validToText, out var parsed))
                return Results.BadRequest(new { error = "Invalid valid_to value." });

            parsedValidTo = parsed;
        }

        var request = new MembershipUpdateRequest(
            id,
            parsedValidFrom,
            parsedValidTo,
            DateTimeOffset.UtcNow,
            actorUserId);

        var updated = await service.UpdateAsync(request, cancellationToken);
        return ApiResults.Ok(updated);
    }


    public static async Task<IResult> Actions(
        Guid id, HttpContext httpContext, IMembershipService service, ICurrentActorProvider actorProvider,
        IRbacAuthorizationService authorizationService, CancellationToken cancellationToken)
    {
        var item = await service.GetByIdAsync(id, cancellationToken);
        if (item is null) return Results.NotFound();
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null) return Results.Unauthorized();
        var canUpdate = await authorizationService.IsAllowedAsync(actor.Value, item.rbacobject_id, RbacOperations.Update, cancellationToken);
        var canDelete = await authorizationService.IsAllowedAsync(actor.Value, item.rbacobject_id, RbacOperations.Delete, cancellationToken);
        var actions = new EntityAction[]
        {
            new("view", "View", $"/architecture/MembershipResponse/view/{id}", Group: "navigation", Order: 10),
            new("edit", "Edit", $"/architecture/MembershipResponse/edit/{id}", EntityActionKind.Update, canUpdate, canUpdate ? null : "Update is not permitted.", "navigation", 20),
            new("delete", "Delete", $"/architecture/MembershipResponse/delete?id={id}", EntityActionKind.Delete, canDelete, canDelete ? null : "Delete is not permitted.", "danger", 90, "DELETE", "Delete this membership?", "/architecture/MembershipResponse/list")
        };
        return ApiResults.Ok(new EntityActionMenuModel(actions));
    }

    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IMembershipService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<MembershipModel, MembershipResponse, IMembershipService>(
            id, httpContext, service, cancellationToken);

    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IMembershipService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<MembershipModel, MembershipResponse, IMembershipService>(id, httpContext, service, cancellationToken);
}

public static class MembershipEndpointExtensions
{
    public static IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/MembershipResponse").WithTags("Membership");

        group.MapPost("/create-contained", MembershipResolvers.CreateContained)
            .WithName("MembershipCreateContained")
            .ApplyJsonModel<MembershipResponse>()
                .WithDescription("""
                    Creates a Membership inside group_id. Create permission is evaluated on that Group; rbacobject_id is derived by the service.
                    """);

        group.MapGet("/create", MembershipResolvers.CreatePage)
            .WithName("MembershipCreatePage")
                .WithDescription("""
                    GET /create. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/create-form", MembershipResolvers.CreateForm)
            .WithName("MembershipCreateForm")
                .WithDescription("""
                    POST /create-form. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);


        group.MapGet("/link/{id:guid}", MembershipResolvers.Link)
            .WithName("MembershipLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>()
                .WithDescription("""
                    GET /link/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/actions/{id:guid}", MembershipResolvers.Actions)
            .WithName("MembershipActions")
            .ApplyViewModel<EntityActionMenu, EntityActionMenuModel>()
            .WithDescription("""Returns the RBAC-aware contextual View/Edit/Delete action menu for a Membership.""");

        group.MapGet("/view/{id:guid}", MembershipResolvers.Read)
            .WithName("MembershipRead")
            .ApplyViewModel<MembershipViewPage, MembershipResponse>()
                .WithDescription("""
                    GET /view/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/edit/{id:guid}", MembershipResolvers.Read)
            .WithName("MembershipEdit")
            .ApplyViewModel<MembershipEditPage, MembershipResponse>()
                .WithDescription("""
                    GET /edit/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/list", MembershipResolvers.List)
            .WithName("MembershipList")
            .ApplyViewModel<MembershipViewListPage, MembershipResponse[]>()
                .WithDescription("""
                    Lists entities through the secured IQueryable. Optional `where` is a Hasura-like JSON filter, `take` limits the result to 1..200 rows, and `operation` selects the RBAC operation used to build the secured query (default: list). This supports incremental entity selectors without introducing a separate lookup API.
                    """);

        group.MapPut("/update", MembershipResolvers.Update)
            .WithName("MembershipUpdate")
            .ApplyJsonModel<MembershipResponse>()
                .WithDescription("""
                    PUT /update. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/update/{id:guid}", MembershipResolvers.UpdateForm)
            .WithName("MembershipUpdateForm")
            .ApplyViewModel<MembershipEditPage, MembershipResponse>()
                .WithDescription("""
                    POST /update/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapDelete("/delete", MembershipResolvers.Delete)
            .WithName("MembershipDelete")
                .WithDescription("""
                    DELETE /delete. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        return endpoints;
    }
}
