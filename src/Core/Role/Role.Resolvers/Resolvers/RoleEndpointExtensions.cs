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

public static class RoleEndpointExtensions
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/RoleResponse").WithTags("Role");

        group.MapGet("", RedirectResolvers.List)
            .WithDescription("Redirects the short entity collection URI to the canonical /list endpoint while preserving query parameters.");

        group.MapGet("/{id:guid}", RedirectResolvers.View)
            .WithDescription("Redirects the short entity URI to the canonical /view/{id} endpoint while preserving query parameters.");

        group.MapPost("/create-contained", RoleResolvers.CreateContained)
            .WithName("RoleCreateContained")
            .ApplyJsonModel<RoleResponse>()
                .WithDescription("""
                    Creates a Role inside group_id. Create permission is evaluated on that Group and the role RBAC scope is derived server-side.
                    """);

        group.MapGet("/create", RoleResolvers.CreatePage)
            .WithName("RoleCreatePage")
            .ApplyViewModel<RoleCreatePage, CreatePageModel>()
                .WithDescription("""
                    GET /create. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/create-form", RoleResolvers.CreateForm)
            .WithName("RoleCreateForm")
            .ApplyViewModel<RoleViewPage, RoleResponse>()
                .WithDescription("""
                    POST /create-form. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);


        group.MapGet("/link/{id:guid}", RoleResolvers.Link)
            .WithName("RoleLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>()
                .WithDescription("""
                    GET /link/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/actions/{id:guid}", RoleResolvers.Actions)
            .WithName("RoleActions")
            .ApplyViewModel<EntityActionMenu, EntityActionMenuModel>()
            .WithDescription("""Returns the RBAC-aware contextual View/Edit/Delete action menu for a Role.""");

        group.MapGet("/view/{id:guid}", RoleResolvers.Read)
            .WithName("RoleRead")
            .ApplyViewModel<RoleViewPage, RoleResponse>()
                .WithDescription("""
                    GET /view/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/edit/{id:guid}", RoleResolvers.Read)
            .WithName("RoleEdit")
            .ApplyViewModel<RoleEditPage, RoleResponse>()
                .WithDescription("""
                    GET /edit/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/list", RoleResolvers.List)
            .WithName("RoleList")
            .ApplyViewModel<RoleViewListPage, RoleResponse[]>()
                .WithDescription("""
                    Lists entities through the secured IQueryable. Optional `where` is a Hasura-like JSON filter, `take` limits the result to 1..200 rows, and `operation` selects the RBAC operation used to build the secured query (default: list). This supports incremental entity selectors without introducing a separate lookup API.
                    """);

        group.MapPut("/update", RoleResolvers.Update)
            .WithName("RoleUpdate")
            .ApplyJsonModel<RoleResponse>()
                .WithDescription("""
                    PUT /update. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/update/{id:guid}", RoleResolvers.UpdateForm)
            .WithName("RoleUpdateForm")
            .ApplyViewModel<RoleEditPage, RoleResponse>()
                .WithDescription("""
                    POST /update/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapDelete("/delete", RoleResolvers.Delete)
            .WithName("RoleDelete")
                .WithDescription("""
                    DELETE /delete. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        return endpoints;
    }
}
