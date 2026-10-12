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
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Membership.UI.Pages;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Resolvers;

/// <summary>
/// Typ Membership Endpoint Extensions ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public static class MembershipEndpointExtensions
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/MembershipResponse").WithTags("Membership");

        group.MapGet("", RedirectResolvers.List)
            .WithDescription("Redirects the short entity collection URI to the canonical /list endpoint while preserving query parameters.");

        group.MapGet("/{id:guid}", RedirectResolvers.View)
            .WithDescription("Redirects the short entity URI to the canonical /view/{id} endpoint while preserving query parameters.");

        group.MapPost("/create-contained", MembershipResolvers.CreateContained)
            .WithName("MembershipCreateContained")
            .ApplyJsonModel<MembershipResponse>()
                .WithDescription("""
                    Creates a Membership inside group_id. Create permission is evaluated on that Group; rbacobject_id is derived by the service.
                    """);

        group.MapGet("/create", MembershipResolvers.CreatePage)
            .WithName("MembershipCreatePage")
            .ApplyViewModel<MembershipCreatePage, CreatePageModel>()
                .WithDescription("""
                    GET /create. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/create-form", MembershipResolvers.CreateForm)
            .WithName("MembershipCreateForm")
            .ApplyViewModel<MembershipViewPage, MembershipResponse>()
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
