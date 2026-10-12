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
/// HTTP resolver pro Group Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
public static class GroupResolvers
{
    public static async Task<IResult> CreatePage(
        ICurrentActorProvider actorProvider,
        IRbacAuthorizationService authorizationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null) return Results.Unauthorized();
        if (Guid.TryParse(httpContext.Request.Query["master_group_id"], out var parentId) &&
            !await authorizationService.IsAllowedAsync(actor.Value, parentId, RbacOperations.Create, cancellationToken))
            return Results.Forbid();
        return ApiResults.Ok(new CreatePageModel("Group", httpContext.Request.Query["group_id"].ToString(), httpContext.Request.Query["return_url"].ToString()));
    }

    public static async Task<IResult> CreateContained(
        GroupCreateContainedRequest request,
        IGroupService service,
        CancellationToken cancellationToken)
        => ApiResults.Ok(await service.CreateContainedAsync(request, cancellationToken));

    public static async Task<IResult> CreateTyped(
        GroupCreateTypedRequest request,
        IGroupService service,
        CancellationToken cancellationToken)
        => ApiResults.Ok(await service.CreateTypedAsync(request, cancellationToken));

    public static async Task<IResult> CreateForm(
        HttpContext httpContext,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var name = form["Name"].ToString();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Name is required." });
        if (!Guid.TryParse(form["group_type_id"].ToString(), out var groupTypeId))
            return Results.BadRequest(new { error = "group_type_id must be a valid Guid." });

        GroupResponse created;
        var parentText = form["master_group_id"].ToString();
        if (Guid.TryParse(parentText, out var parentId))
            created = await service.CreateContainedAsync(
                new GroupCreateContainedRequest(name, parentId, groupTypeId), cancellationToken);
        else
            created = await service.CreateTypedAsync(
                new GroupCreateTypedRequest(name, groupTypeId), cancellationToken);

        var returnUrl = form["return_url"].ToString();
        return ApiResults.Ok(created);
    }

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Read, List; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Read(Guid id, HttpContext httpContext, IGroupService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<GroupModel, GroupResponse, IGroupService>(id, httpContext, service, cancellationToken);

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: List; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> List(HttpContext httpContext, IGroupService service, CancellationToken cancellationToken)
        => BaseResolvers.List<GroupModel, GroupResponse, IGroupService>(httpContext, service, cancellationToken);

    public static async Task<IResult> Update(
        GroupUpdateRequest request,
        HttpContext httpContext,
        IGroupService service,
        CancellationToken cancellationToken)
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

        return await BaseResolvers.Update<GroupModel, GroupResponse, GroupUpdateRequest, IGroupService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> UpdateForm(
        Guid id,
        HttpContext httpContext,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var name = form["Name"].ToString();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Name is required." });

        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actorUserId = actorProvider.TryGetActorUserId(httpContext);
        if (actorUserId is null)
            return Results.Unauthorized();

        var existing = await service.GetByIdAsync<GroupResponse>(id, cancellationToken);
        if (existing is null)
            return Results.NotFound();

        var authorization = await BaseResolvers.AuthorizeModelAsync(
            httpContext,
            existing,
            RbacOperations.Update,
            cancellationToken);

        if (authorization is not null)
            return authorization;

        var request = new GroupUpdateRequest(
            id,
            name,
            existing.group_type_id,
            existing.master_group_id,
            existing.inherit_roles,
            DateTimeOffset.UtcNow,
            actorUserId);

        var updated = await service.UpdateAsync<GroupUpdateRequest, GroupResponse>(request, cancellationToken);
        return ApiResults.Ok(updated);
    }


    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IGroupService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<GroupModel, GroupResponse, IGroupService>(
            id, httpContext, service, cancellationToken);

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: Delete; pořadí a parametry viz implementace. Návratová hodnota reprezentuje HTTP výsledek (případně přesměrování či chybu).
    /// </summary>
    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IGroupService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<GroupModel, GroupResponse, IGroupService>(id, httpContext, service, cancellationToken);

    public static async Task<IResult> Actions(
        Guid id,
        HttpContext httpContext,
        IGroupService groupService,
        ICurrentActorProvider actorProvider,
        IRbacAuthorizationService authorizationService,
        CancellationToken cancellationToken)
    {
        var item = await groupService.GetByIdAsync<GroupResponse>(id, cancellationToken);
        if (item is null) return Results.NotFound();

        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null) return Results.Unauthorized();

        var canCreate = await authorizationService.IsAllowedAsync(actor.Value, item.rbacobject_id, RbacOperations.Create, cancellationToken);
        var canUpdate = await authorizationService.IsAllowedAsync(actor.Value, item.rbacobject_id, RbacOperations.Update, cancellationToken);
        var canDelete = await authorizationService.IsAllowedAsync(actor.Value, item.rbacobject_id, RbacOperations.Delete, cancellationToken);
        var returnUrl = $"/architecture/GroupResponse/view/{id}";

        var actions = new EntityAction[]
        {
            new("view", "View", returnUrl, EntityActionKind.Navigate, Group: "navigation", Order: 10),
            new("edit", "Edit", $"/architecture/GroupResponse/edit/{id}", EntityActionKind.Update, canUpdate,
                canUpdate ? null : "You do not have update permission for this group.", "navigation", 20),
            new("add-member", "Add member", $"/architecture/MembershipResponse/create?group_id={id}&return_url={Uri.EscapeDataString(returnUrl)}",
                EntityActionKind.Relation, canCreate, canCreate ? null : "You do not have permission to add a member to this group.", "relations", 30),
            new("assign-role", "Assign role", $"/architecture/RoleResponse/create?group_id={id}&return_url={Uri.EscapeDataString(returnUrl)}",
                EntityActionKind.Relation, canCreate, canCreate ? null : "You do not have permission to assign a role in this group.", "relations", 40),
            new("add-child", "Add child group", $"/architecture/GroupResponse/create?master_group_id={id}&return_url={Uri.EscapeDataString(returnUrl)}",
                EntityActionKind.Create, canCreate, canCreate ? null : "You do not have permission to create a child group here.", "relations", 50),
            new("delete", "Delete", $"/architecture/GroupResponse/delete?id={id}", EntityActionKind.Delete, canDelete,
                canDelete ? null : "You do not have delete permission for this group.", "danger", 90, "DELETE",
                "Delete this group?", "/architecture/GroupResponse/list")
        };

        return ApiResults.Ok(new EntityActionMenuModel(actions));
    }

    public static async Task<IResult> Roles(
        Guid id,
        HttpContext httpContext,
        IGroupService groupService,
        IRoleService roleService,
        CancellationToken cancellationToken)
    {
        var group = await groupService.GetByIdAsync<GroupResponse>(id, cancellationToken);
        if (group is null)
            return Results.NotFound();

        IReadOnlyCollection<RoleResponse> roles = Array.Empty<RoleResponse>();
        VectorPageInfo? pageInfo = null;
        if (ShouldLoadVector(httpContext))
        {
            if (!VectorQueryParser.TryParse<RoleModel>(
                httpContext,
                role => role.group_id == id,
                out var predicate,
                out var options,
                out var error))
            {
                return Results.BadRequest(new { error });
            }

            var page = await roleService.VectorPageAsync<RoleResponse>(
                predicate, RbacOperations.List, options, cancellationToken);
            roles = page.Items;
            pageInfo = VectorQueryParser.WithRequestState(httpContext, page.PageInfo);
            VectorQueryParser.WritePagingHeaders(httpContext, pageInfo);
        }

        return ApiResults.Ok(new EntityRelationsViewModel<GroupResponse, RoleResponse>(
            group,
            roles,
            "Skupina: role",
            "Roles",
            pageInfo));
    }

    public static async Task<IResult> Memberships(
        Guid id,
        HttpContext httpContext,
        IGroupService groupService,
        IMembershipService membershipService,
        CancellationToken cancellationToken)
    {
        var group = await groupService.GetByIdAsync<GroupResponse>(id, cancellationToken);
        if (group is null)
            return Results.NotFound();

        IReadOnlyCollection<MembershipResponse> memberships = Array.Empty<MembershipResponse>();
        VectorPageInfo? pageInfo = null;
        if (ShouldLoadVector(httpContext))
        {
            if (!VectorQueryParser.TryParse<MembershipModel>(
                httpContext,
                membership => membership.group_id == id,
                out var predicate,
                out var options,
                out var error))
            {
                return Results.BadRequest(new { error });
            }

            var page = await membershipService.VectorPageAsync<MembershipResponse>(
                predicate, RbacOperations.List, options, cancellationToken);
            memberships = page.Items;
            pageInfo = VectorQueryParser.WithRequestState(httpContext, page.PageInfo);
            VectorQueryParser.WritePagingHeaders(httpContext, pageInfo);
        }

        return ApiResults.Ok(new EntityRelationsViewModel<GroupResponse, MembershipResponse>(
            group,
            memberships,
            "Skupina: členství",
            "Memberships",
            pageInfo));
    }

    private static bool ShouldLoadVector(HttpContext httpContext)
    {
        if (string.Equals(httpContext.Request.Headers["HX-Request"].ToString(), "true", StringComparison.OrdinalIgnoreCase))
            return true;

        var explicitMode = httpContext.Request.Headers["X-Response-Type"].ToString();
        if (string.Equals(explicitMode, "fragment", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(explicitMode, "json", StringComparison.OrdinalIgnoreCase))
            return true;

        return httpContext.Request.GetTypedHeaders().Accept?.Any(media =>
            string.Equals(media.MediaType.Value, "application/json", StringComparison.OrdinalIgnoreCase)) == true;
    }
}
