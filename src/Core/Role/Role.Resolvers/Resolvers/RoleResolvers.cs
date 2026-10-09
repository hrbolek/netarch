using WebAppArchitecture.Base.UI.Pages;
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
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.Role.UI.Pages;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Role.Resolvers;

public static class RoleResolvers
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
        return ApiResults.Ok(new CreatePageModel("Role", httpContext.Request.Query["group_id"].ToString(), httpContext.Request.Query["return_url"].ToString()));
    }

    public static async Task<IResult> CreateContained(
        RoleCreateContainedRequest request,
        IRoleService service,
        CancellationToken cancellationToken)
        => ApiResults.Ok(await service.CreateContainedAsync(request, cancellationToken));

    public static async Task<IResult> CreateForm(
        HttpContext httpContext,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        if (!Guid.TryParse(form["group_id"].ToString(), out var groupId))
            return Results.BadRequest(new { error = "Invalid group_id value." });
        if (!Guid.TryParse(form["user_id"].ToString(), out var userId))
            return Results.BadRequest(new { error = "Invalid user_id value." });
        if (!Guid.TryParse(form["role_type_id"].ToString(), out var roleTypeId))
            return Results.BadRequest(new { error = "role_type_id must be a valid Guid." });
        var isDeputy = string.Equals(form["is_deputy"].ToString(), "true", StringComparison.OrdinalIgnoreCase);
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
            new RoleCreateContainedRequest(groupId, userId, roleTypeId, isDeputy, validFrom, validTo), cancellationToken);
        var returnUrl = form["return_url"].ToString();
        return ApiResults.Ok(created);
    }

    public static Task<IResult> Read(Guid id, HttpContext httpContext, IRoleService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<RoleModel, RoleResponse, IRoleService>(id, httpContext, service, cancellationToken);

    public static Task<IResult> List(HttpContext httpContext, IRoleService service, CancellationToken cancellationToken)
        => BaseResolvers.List<RoleModel, RoleResponse, IRoleService>(httpContext, service, cancellationToken);

    public static async Task<IResult> Update(RoleUpdateRequest request, HttpContext httpContext, IRoleService service, CancellationToken cancellationToken)
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

        return await BaseResolvers.Update<RoleModel, RoleResponse, RoleUpdateRequest, IRoleService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> UpdateForm(
        Guid id,
        HttpContext httpContext,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var roleTypeValue = form["role_type_id"].ToString();
        _ = Guid.TryParse(roleTypeValue, out var roleTypeId);
        var isDeputyText = form["is_deputy"].ToString();
        var validFromText = form["valid_from"].ToString();
        var validToText = form["valid_to"].ToString();

        if (roleTypeId == Guid.Empty)
            return Results.BadRequest(new { error = "role_type_id must be a valid Guid." });

        if (!bool.TryParse(isDeputyText, out var isDeputy))
            return Results.BadRequest(new { error = "Invalid is_deputy value." });

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

        var request = new RoleUpdateRequest(
            id,
            roleTypeId,
            isDeputy,
            parsedValidFrom,
            parsedValidTo,
            DateTimeOffset.UtcNow,
            actorUserId);

        var updated = await service.UpdateAsync(request, cancellationToken);
        return ApiResults.Ok(updated);
    }


    public static async Task<IResult> Actions(
        Guid id, HttpContext httpContext, IRoleService service, ICurrentActorProvider actorProvider,
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
            new("view", "View", $"/architecture/RoleResponse/view/{id}", Group: "navigation", Order: 10),
            new("edit", "Edit", $"/architecture/RoleResponse/edit/{id}", EntityActionKind.Update, canUpdate, canUpdate ? null : "Update is not permitted.", "navigation", 20),
            new("delete", "Delete", $"/architecture/RoleResponse/delete?id={id}", EntityActionKind.Delete, canDelete, canDelete ? null : "Delete is not permitted.", "danger", 90, "DELETE", "Delete this role?", "/architecture/RoleResponse/list")
        };
        return ApiResults.Ok(new EntityActionMenuModel(actions));
    }

    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IRoleService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<RoleModel, RoleResponse, IRoleService>(
            id, httpContext, service, cancellationToken);

    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IRoleService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<RoleModel, RoleResponse, IRoleService>(id, httpContext, service, cancellationToken);
}
