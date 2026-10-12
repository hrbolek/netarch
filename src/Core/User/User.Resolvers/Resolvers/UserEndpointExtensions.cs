using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.Base.UI.Vector;

using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Services;
using WebAppArchitecture.User.UI.Pages;
using WebAppArchitecture.User.UI.Components.Vectors;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Group.Services;

namespace WebAppArchitecture.User.Resolvers;

/// <summary>
/// Typ User Endpoint Extensions ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public static class UserEndpointExtensions
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix + "/UserResponse")
            .WithTags("User");

        group.MapGet("", RedirectResolvers.List)
            .WithDescription("Redirects the short entity collection URI to the canonical /list endpoint while preserving query parameters.");

        group.MapGet("/{id:guid}", RedirectResolvers.View)
            .WithDescription("Redirects the short entity URI to the canonical /view/{id} endpoint while preserving query parameters.");

        group.MapPost("/create-contained", UserResolvers.CreateContained)
            .WithName("UserCreateContained")
            .ApplyJsonModel<UserResponse>()
                .WithDescription("""
                    Creates a user inside group_id and immediately creates its Membership. Create permission is evaluated on the target Group; no client-supplied RBAC object is accepted.
                    """);

        group.MapGet("/create", UserResolvers.CreatePage)
            .WithName("UserCreatePage")
            .ApplyViewModel<UserCreatePage, CreatePageModel>()
                .WithDescription("""
                    GET /create. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/create-form", UserResolvers.CreateForm)
            .WithName("UserCreateForm")
            .ApplyViewModel<UserViewPage, UserResponse>()
                .WithDescription("""
                    POST /create-form. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);


        group.MapGet("/link/{id:guid}", UserResolvers.Link)
            .WithName("UserLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>()
                .WithDescription("""
                    GET /link/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/actions/{id:guid}", UserResolvers.Actions)
            .WithName("UserActions")
            .ApplyViewModel<EntityActionMenu, EntityActionMenuModel>()
            .WithDescription("""
                Returns the contextual action menu for a User. Standard View/Edit/Delete actions are combined with relation actions. Enabled state is calculated from the acting user's RBAC permissions; target-dependent relation actions are enabled only when at least one permitted target Group exists.
                """);

        group.MapGet("/view/{id:guid}", UserResolvers.Read)
            .WithName("UserRead")
            .ApplyViewModel<UserViewPage, UserResponse>()
                .WithDescription("""
                    GET /view/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/edit/{id:guid}", UserResolvers.Read)
            .WithName("UserEdit")
            .ApplyViewModel<UserEditPage, UserResponse>()
                .WithDescription("""
                    GET /edit/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/list", UserResolvers.List)
            .WithName("UserList")
            .ApplyViewModel<UserViewListPage, UserResponse[]>()
                .WithDescription("""
                    Lists secured users. Optional `where` accepts a JSON Hasura-like filter expression
                    that is translated to an IQueryable predicate before database materialization. Optional `take`
                    limits the result to 1..200 rows and is intended for selectors and incremental lookup UI.
                    """);

        group.MapGet("/memberships/{id:guid}", UserResolvers.Memberships)
            .WithName("UserMemberships")
            .ApplyViewModel<UserVectorPage<UserMembershipVectorContent<TableVectorContent>>, EntityRelationsViewModel<UserResponse, MembershipResponse>>()
                .WithDescription("""
                    GET /memberships/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/membershipscards/{id:guid}", UserResolvers.Memberships)
            .WithName("UserMembershipsCards")
            .ApplyViewModel<UserVectorPage<UserMembershipVectorContent<CardVectorContent>>, EntityRelationsViewModel<UserResponse, MembershipResponse>>()
            .WithDescription("""
                Alternative card presentation of the same User Membership vector. It uses the same resolver, Hasura-like filter query parameters, lazy HTMX loading, and RBAC filtering as /memberships/{id}.
                """);

        group.MapGet("/roles/{id:guid}", UserResolvers.Roles)
            .WithName("UserRoles")
            .ApplyViewModel<UserVectorPage<UserRoleVectorContent<TableVectorContent>>, EntityRelationsViewModel<UserResponse, RoleResponse>>()
                .WithDescription("""
                    GET /roles/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        // Structured Hasura-like filters are sent as JSON in the request body.
        group.MapPost("/vector", UserResolvers.Vector)
            .WithName("UserVector")
            .ApplyJsonModel<UserResponse[]>()
                .WithDescription("""
                    POST /vector. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPut("/update", UserResolvers.Update)
            .WithName("UserUpdate")
            .ApplyJsonModel<UserResponse>()
                .WithDescription("""
                    PUT /update. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/update/{id:guid}", UserResolvers.UpdateForm)
            .WithName("UserUpdateForm")
            .ApplyViewModel<UserEditPage, UserResponse>()
                .WithDescription("""
                    POST /update/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapDelete("/delete", UserResolvers.Delete)
            .WithName("UserDelete")
                .WithDescription("""
                    DELETE /delete. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        return endpoints;
    }
}
