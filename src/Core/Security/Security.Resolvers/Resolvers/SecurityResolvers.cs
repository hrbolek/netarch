using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Security.Models;
using WebAppArchitecture.Security.Services;
using WebAppArchitecture.Security.UI.Pages;
using WebAppArchitecture.Security.UI.Components;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.Security.Resolvers;

/// <summary>
/// HTTP resolver pro Security Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
/// <remarks>
/// Architecture exception: authorization decisions and the scope-authorized permission projection
/// use SecurityService/SecurityPermissionService rather than SecuredBaseService entity CRUD.
/// The former is below ISecurityLayer and must not depend recursively on it; the latter explains
/// grants only after Read on the requested scope is checked here. Role mutations still delegate
/// to the secured IRoleService. This is not an exception for direct resolver database access.
/// </remarks>
public static class SecurityResolvers
{
    public static async Task<IResult> Operations(
        Guid rbacId,
        Guid? userId,
        HttpContext httpContext,
        ISecurityService service,
        ICurrentActorProvider actorProvider,
        CancellationToken cancellationToken)
    {
        var effectiveUserId = userId ?? actorProvider.TryGetActorUserId(httpContext);
        if (effectiveUserId is null)
            return Results.Unauthorized();

        var operations = await service.GetAllowedOperationsAsync(
            effectiveUserId.Value,
            rbacId,
            cancellationToken);

        return ApiResults.Ok(
            operations
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    public static async Task<IResult> Permissions(
        Guid rbacobjectId,
        HttpContext httpContext,
        ISecurityService securityService,
        ISecurityPermissionService permissionService,
        ICurrentActorProvider actorProvider,
        CancellationToken cancellationToken)
    {
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null)
            return Results.Unauthorized();

        var canRead = await securityService.IsAllowedAsync(
            actor.Value,
            rbacobjectId,
            SecurityOperations.Read,
            cancellationToken);

        if (!canRead)
            return Results.Forbid();

        var model = await permissionService.GetPermissionsAsync(
            rbacobjectId,
            cancellationToken);

        return ApiResults.Ok(model);
    }
    public static Task<IResult> RootPermissions(
        HttpContext httpContext,
        ISecurityService securityService,
        ISecurityPermissionService permissionService,
        ICurrentActorProvider actorProvider,
        CancellationToken cancellationToken)
        => Permissions(
            RbacWellKnown.RootGroupId,
            httpContext,
            securityService,
            permissionService,
            actorProvider,
            cancellationToken);

    public static async Task<IResult> CreateRoleAction(
        Guid rbacobjectId,
        string? return_url,
        HttpContext httpContext,
        ISecurityService securityService,
        ICurrentActorProvider actorProvider,
        CancellationToken cancellationToken)
    {
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null)
            return Results.Unauthorized();

        var canCreateRole = await securityService.IsAllowedAsync(
            actor.Value,
            rbacobjectId,
            SecurityOperations.Create,
            cancellationToken);

        var model = new RoleAssignmentActionResponse(
            rbacobjectId,
            string.IsNullOrWhiteSpace(return_url) ? "/architecture" : return_url,
            canCreateRole);

        return ApiResults.Ok(model);
    }

    public static async Task<IResult> CreateRoleDialog(
        Guid rbacobjectId,
        string? return_url,
        HttpContext httpContext,
        ISecurityService securityService,
        ICurrentActorProvider actorProvider,
        CancellationToken cancellationToken)
    {
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null)
            return Results.Unauthorized();

        var canCreateRole = await securityService.IsAllowedAsync(
            actor.Value,
            rbacobjectId,
            SecurityOperations.Create,
            cancellationToken);

        if (!canCreateRole)
            return Results.Forbid();

        var model = new RoleAssignmentDialogResponse(
            rbacobjectId,
            string.IsNullOrWhiteSpace(return_url) ? "/architecture" : return_url,
            DateTimeOffset.Now);
        return ApiResults.Ok(model);
    }

    public static async Task<IResult> CreateRoleForm(
        Guid rbacobjectId,
        HttpContext httpContext,
        IRoleService roleService,
        ISecurityService securityService,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        if (!Guid.TryParse(form["user_id"].ToString(), out var userId))
            return Results.BadRequest(new { error = "user_id must be a valid Guid." });
        if (!Guid.TryParse(form["role_type_id"].ToString(), out var roleTypeId))
            return Results.BadRequest(new { error = "role_type_id must be a valid Guid." });
        if (!DateTimeOffset.TryParse(form["valid_from"].ToString(), out var validFrom))
            return Results.BadRequest(new { error = "valid_from must be a valid date/time." });

        DateTimeOffset? validTo = null;
        var validToText = form["valid_to"].ToString();
        if (!string.IsNullOrWhiteSpace(validToText))
        {
            if (!DateTimeOffset.TryParse(validToText, out var parsedValidTo))
                return Results.BadRequest(new { error = "valid_to must be a valid date/time." });
            validTo = parsedValidTo;
            if (validTo < validFrom)
                return Results.BadRequest(new { error = "valid_to cannot be earlier than valid_from." });
        }

        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null) return Results.Unauthorized();

        var allowed = await securityService.IsAllowedAsync(actor.Value, rbacobjectId, SecurityOperations.Create, cancellationToken);
        if (!allowed) return Results.Forbid();

        await roleService.CreateContainedAsync(new RoleCreateContainedRequest(
            group_id: rbacobjectId,
            user_id: userId,
            role_type_id: roleTypeId,
            is_deputy: string.Equals(form["is_deputy"].ToString(), "true", StringComparison.OrdinalIgnoreCase),
            valid_from: validFrom,
            valid_to: validTo), cancellationToken);

        var returnUrl = form["return_url"].ToString();
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/')) returnUrl = "/architecture";
        return Results.Redirect(returnUrl);
    }

}
