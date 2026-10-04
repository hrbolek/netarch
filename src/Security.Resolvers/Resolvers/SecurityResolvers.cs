using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Security.Models;
using WebAppArchitecture.Security.Services;
using WebAppArchitecture.Security.UI.Pages;
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

        return endpoints;
    }
}
