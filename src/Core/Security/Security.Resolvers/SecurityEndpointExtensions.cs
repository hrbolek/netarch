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
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.Security.Resolvers;

/// <summary>
/// Typ Security Endpoint Extensions ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>

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
            .ApplyJsonModel<string[]>()
                .WithDescription("""
                    GET /{rbacId:guid}/operations. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        var permissions = endpoints
            .MapGroup(prefix.TrimEnd('/') + "/RbacPermissionResponse")
            .WithTags("Security");

        // Secured administration view; uses the exact EntityPermissions component from entity detail pages.
        endpoints.MapGet(prefix.TrimEnd('/') + "/root-permissions", SecurityResolvers.RootPermissions)
            .WithName("SecurityRootPermissions")
            .WithDescription("Shows root RBAC role assignments and creation scopes; only readable by an actor with Read on Root.")
            .ApplyViewModel<RootPermissionAdministrationPage, RbacPermissionResponse>();

        permissions.MapGet("/permissions/{rbacobjectId:guid}", SecurityResolvers.Permissions)
            .WithName("SecurityPermissions")
            .ApplyViewModel<RbacPermissionView, RbacPermissionResponse>()
                .WithDescription("""
                    GET /permissions/{rbacobjectId:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        permissions.MapGet("/create-role-action/{rbacobjectId:guid}", SecurityResolvers.CreateRoleAction)
            .WithName("SecurityCreateRoleAction")
            .WithDescription("""
                Returns the RBAC-aware UI action for adding a role to the current object.
                The response contains an active Add role button only when the current actor
                has the create operation on the target RBAC object; otherwise it renders a
                disabled action and no dialog-loading URL is exposed.
                """)
            .ApplyViewModel<RoleAssignmentAction, RoleAssignmentActionResponse>();

        permissions.MapGet("/create-role/{rbacobjectId:guid}", SecurityResolvers.CreateRoleDialog)
            .WithName("SecurityCreateRoleDialog")
            .WithDescription("""
                Returns the Bootstrap dialog content used to create a time-limited role
                directly on the current RBAC object. User and role-type choices are loaded
                lazily from their standard secured list endpoints using Hasura-like where filters.
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
