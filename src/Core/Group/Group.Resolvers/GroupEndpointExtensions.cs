using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.Base.UI.Vector;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.Group.Services;
using WebAppArchitecture.Group.UI.Pages;
using WebAppArchitecture.Group.UI.Components.Vectors;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Resolvers;

/// <summary>
/// Typ Group Endpoint Extensions ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>

public static class GroupEndpointExtensions
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/GroupResponse").WithTags("Group");

        group.MapGet("", RedirectResolvers.List)
            .WithDescription("Redirects the short entity collection URI to the canonical /list endpoint while preserving query parameters.");

        group.MapGet("/{id:guid}", RedirectResolvers.View)
            .WithDescription("Redirects the short entity URI to the canonical /view/{id} endpoint while preserving query parameters.");

        group.MapPost("/create-contained", GroupResolvers.CreateContained)
            .WithName("GroupCreateContained")
            .ApplyJsonModel<GroupResponse>()
                .WithDescription("""
                    Creates a child Group below master_group_id. Create permission is evaluated on the parent Group; the new Group receives its own RBAC object.
                    """);

        group.MapPost("/create-typed", GroupResolvers.CreateTyped)
            .WithName("GroupCreateTyped")
            .ApplyJsonModel<GroupResponse>()
            .WithDescription("""
                Creates a root Group. Create permission is evaluated on group_type_id; the new Group receives its own RBAC object.
                """);

        group.MapGet("/create", GroupResolvers.CreatePage)
            .WithName("GroupCreatePage")
            .ApplyViewModel<GroupCreatePage, CreatePageModel>()
                .WithDescription("""
                    GET /create. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/create-form", GroupResolvers.CreateForm)
            .WithName("GroupCreateForm")
            .ApplyViewModel<GroupViewPage, GroupResponse>()
                .WithDescription("""
                    POST /create-form. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/link/{id:guid}", BaseResolvers.Link<GroupModel, GroupResponse, IGroupService>)
            .WithName("GroupLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>()
                .WithDescription("""
                    GET /link/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/actions/{id:guid}", GroupResolvers.Actions)
            .WithName("GroupActions")
            .ApplyViewModel<EntityActionMenu, EntityActionMenuModel>()
            .WithDescription("""
                Returns the contextual action menu for a Group. The resolver composes standard View/Edit/Delete actions with Add member, Assign role and Add child group. CUD actions are enabled only when the acting user has the corresponding RBAC permission on the Group.
                """);

        group.MapGet("/view/{id:guid}", BaseResolvers.Read<GroupModel, GroupResponse, IGroupService>)
            .WithName("GroupRead")
            .ApplyViewModel<GroupViewPage, GroupResponse>()
                .WithDescription("""
                    GET /view/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/edit/{id:guid}", BaseResolvers.Read<GroupModel, GroupResponse, IGroupService>)
            .WithName("GroupEdit")
            .ApplyViewModel<GroupEditPage, GroupResponse>()
                .WithDescription("""
                    GET /edit/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/list", BaseResolvers.List<GroupModel, GroupResponse, IGroupService>)
            .WithName("GroupList")
            .ApplyViewModel<GroupViewListPage, GroupResponse[]>()
                .WithDescription("""
                    Lists entities readable by the current actor. Optional `where` is a Hasura-like JSON filter, and `take` limits the result to 1..200 rows.
                    """);

        group.MapGet("/create-targets", BaseResolvers.ListForCreate<GroupModel, GroupResponse, IGroupService>)
            .WithName("GroupCreateTargets")
            .ApplyJsonModel<GroupResponse[]>()
            .WithDescription("""
                Lists groups on which the current actor has Create permission, for selecting a target when creating a related entity.
                Optional `where` is a Hasura-like JSON filter and `take` limits the result to 1..200 rows.
                """);

        group.MapGet("/roles/{id:guid}", GroupResolvers.Roles)
            .WithName("GroupRoles")
            .ApplyViewModel<GroupVectorPage<GroupRoleVectorContent<TableVectorContent>>, EntityRelationsViewModel<GroupResponse, RoleResponse>>()
                .WithDescription("""
                    GET /roles/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/memberships/{id:guid}", GroupResolvers.Memberships)
            .WithName("GroupMemberships")
            .ApplyViewModel<GroupVectorPage<GroupMembershipVectorContent<TableVectorContent>>, EntityRelationsViewModel<GroupResponse, MembershipResponse>>()
                .WithDescription("""
                    GET /memberships/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/membershipscards/{id:guid}", GroupResolvers.Memberships)
            .WithName("GroupMembershipsCards")
            .ApplyViewModel<GroupVectorPage<GroupMembershipVectorContent<CardVectorContent>>, EntityRelationsViewModel<GroupResponse, MembershipResponse>>()
            .WithDescription("""
                Alternative card presentation of the same Group Membership vector. It uses the same resolver, Hasura-like filter query parameters, lazy HTMX loading, and RBAC filtering as /memberships/{id}.
                """);

        group.MapPut("/update", BaseResolvers.Update<GroupModel, GroupResponse, GroupUpdateRequest, IGroupService>)
            .WithName("GroupUpdate")
            .ApplyJsonModel<GroupResponse>()
                .WithDescription("""
                    PUT /update. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/update/{id:guid}", GroupResolvers.UpdateForm)
            .WithName("GroupUpdateForm")
            .ApplyViewModel<GroupEditPage, GroupResponse>()
                .WithDescription("""
                    POST /update/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapDelete("/delete", BaseResolvers.Delete<GroupModel, GroupResponse, IGroupService>)
            .WithName("GroupDelete")
                .WithDescription("""
                    DELETE /delete. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        return endpoints;
    }
}
