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
using WebAppArchitecture.User.Services;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.RoleType.Services;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.Security.Resolvers;

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
    public static Task<IResult> CreateRoleDialog(
        Guid rbacobjectId,
        string? return_url,
        IUserService userService,
        IRoleTypeService roleTypeService,
        CancellationToken cancellationToken)
    {
        var users = userService.Query()
            .Select(x => new RoleAssignmentOption(x.id, x.visual_label))
            .ToArray()
            .OrderBy(x => x.label, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var roleTypes = roleTypeService.Query()
            .Select(x => new RoleAssignmentOption(x.id, x.visual_label))
            .ToArray()
            .OrderBy(x => x.label, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var model = new RoleAssignmentDialogResponse(
            rbacobjectId,
            string.IsNullOrWhiteSpace(return_url) ? "/architecture" : return_url,
            users,
            roleTypes,
            DateTimeOffset.Now);
        return Task.FromResult<IResult>(ApiResults.Ok(model));
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

        var now = DateTimeOffset.UtcNow;
        await roleService.CreateAsync(new RoleCreateRequest(
            group_id: rbacobjectId,
            user_id: userId,
            role_type_id: roleTypeId,
            is_deputy: string.Equals(form["is_deputy"].ToString(), "true", StringComparison.OrdinalIgnoreCase),
            valid_from: validFrom,
            valid_to: validTo,
            rbacobject_id: rbacobjectId,
            created_at: now,
            updated_at: now,
            created_by: actor,
            updated_by: actor), cancellationToken);

        var returnUrl = form["return_url"].ToString();
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/')) returnUrl = "/architecture";
        return Results.Redirect(returnUrl);
    }

}

public static class SecurityEndpointExtensions
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix)
    {
        var security = endpoints
            .MapGroup(prefix.TrimEnd('/') + "/security")
            .WithTags("Security");

        security.MapGet("/{rbacId:guid}/operations", SecurityResolvers.Operations)
            .WithName("SecurityOperations")
            .ApplyJsonModel<string[]>();

        var permissions = endpoints
            .MapGroup(prefix.TrimEnd('/') + "/RbacPermissionResponse")
            .WithTags("Security");

        permissions.MapGet("/permissions/{rbacobjectId:guid}", SecurityResolvers.Permissions)
            .WithName("SecurityPermissions")
            .ApplyViewModel<RbacPermissionView, RbacPermissionResponse>();

        permissions.MapGet("/create-role/{rbacobjectId:guid}", SecurityResolvers.CreateRoleDialog)
            .WithName("SecurityCreateRoleDialog")
            .WithDescription("""
                Returns the Bootstrap dialog content used to create a time-limited role
                directly on the current RBAC object. Users and role types are loaded
                from the secured application services.
                """)
            .ApplyViewModel<RoleAssignmentDialog, RoleAssignmentDialogResponse>();

        permissions.MapPost("/create-role-form/{rbacobjectId:guid}", SecurityResolvers.CreateRoleForm)
            .WithName("SecurityCreateRoleForm")
            .WithDescription("""
                Creates a role on the selected RBAC object with an explicit validity interval
                and redirects the browser to the page from which role creation was initiated.
                """);

        return endpoints;
    }
}
