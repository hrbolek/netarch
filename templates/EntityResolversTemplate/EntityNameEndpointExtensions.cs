using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.UI.Components;
using RootNamespace.EntityName.Models;
using RootNamespace.EntityName.UI.Pages;

namespace RootNamespace.EntityName.Resolvers;

/// <summary>Canonical Minimal API endpoint declarations for EntityName.</summary>

public static class EntityNameEndpointExtensions
{
    public static IEndpointRouteBuilder MapEntityNameEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/EntityNameResponse").WithTags("EntityName");

        group.MapGet("", RedirectResolvers.List)
            .WithDescription("Redirects the collection URI to /list, preserving query parameters.");
        group.MapGet("/{id:guid}", RedirectResolvers.View)
            .WithDescription("Redirects the entity URI to /view/{id}, preserving query parameters.");

        group.MapGet("/list", EntityNameResolvers.List)
            .WithName("EntityNameList")
            .ApplyViewModel<EntityNameViewListPage, EntityNameResponse[]>()
            .WithDescription("Lists entities visible to the acting user, with supported filtering and paging.");

        group.MapGet("/view/{id:guid}", EntityNameResolvers.Read)
            .WithName("EntityNameRead")
            .ApplyViewModel<EntityNameViewPage, EntityNameResponse>()
            .WithDescription("Returns an RBAC-authorized entity. Default representation is HTML; JSON is negotiable.");

        group.MapGet("/edit/{id:guid}", EntityNameResolvers.Read)
            .WithName("EntityNameEdit")
            .ApplyViewModel<EntityNameEditPage, EntityNameResponse>()
            .WithDescription("Renders the entity edit page using the standard PageEdit composition.");

        group.MapGet("/link/{id:guid}", EntityNameResolvers.Link)
            .WithName("EntityNameLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>()
            .WithDescription("Returns a compact, RBAC-aware entity link or a JSON link model.");

        group.MapPut("/update", EntityNameResolvers.Update)
            .WithName("EntityNameUpdate")
            .ApplyJsonModel<EntityNameResponse>()
            .WithDescription("Updates an entity after service-layer validation and RBAC authorization.");

        group.MapDelete("/delete/{id:guid}", EntityNameResolvers.Delete)
            .WithName("EntityNameDelete")
            .WithDescription("Deletes an entity when the acting user has delete permission.");

        // REQUIRED FOR A COMPLETE ENTITY: add /create (HTML), /create-contained (JSON),
        // /create-form (HTML), /update/{id} (form), /actions/{id} and /vector (JSON)
        // using real service contracts, DTOs and UI components. NEVER expose placeholder
        // handlers that bypass RBAC or invent response types to satisfy a template.
        return endpoints;
    }
}
