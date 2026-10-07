using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.Group.Services;
using WebAppArchitecture.Group.UI.Pages;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Resolvers;

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
        return new RazorComponentResult<GroupCreatePage>();
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
        return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl)
            ? $"/architecture/GroupResponse/edit/{created.id}"
            : returnUrl);
    }

    public static Task<IResult> Read(Guid id, HttpContext httpContext, IGroupService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<GroupModel, GroupResponse, IGroupService>(id, httpContext, service, cancellationToken);

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

        var existing = await service.GetByIdAsync(id, cancellationToken);
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

        var updated = await service.UpdateAsync(request, cancellationToken);
        return ApiResults.Ok(updated);
    }


    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IGroupService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<GroupModel, GroupResponse, IGroupService>(
            id, httpContext, service, cancellationToken);

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
        var item = await groupService.GetByIdAsync(id, cancellationToken);
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
        var group = await groupService.GetByIdAsync(id, cancellationToken);
        if (group is null)
            return Results.NotFound();

        var roles = await roleService.VectorAsync(role => role.group_id == id, cancellationToken);

        return ApiResults.Ok(new EntityRelationsViewModel<GroupResponse, RoleResponse>(
            group,
            roles,
            "Skupina: role",
            "Roles"));
    }

    public static async Task<IResult> Memberships(
        Guid id,
        HttpContext httpContext,
        IGroupService groupService,
        IMembershipService membershipService,
        CancellationToken cancellationToken)
    {
        var group = await groupService.GetByIdAsync(id, cancellationToken);
        if (group is null)
            return Results.NotFound();

        var memberships = await membershipService.VectorAsync(membership => membership.group_id == id, cancellationToken);

        return ApiResults.Ok(new EntityRelationsViewModel<GroupResponse, MembershipResponse>(
            group,
            memberships,
            "Skupina: členství",
            "Memberships"));
    }
}

public static class GroupTypeResolvers
{
    public static Task<IResult> Read(Guid id, HttpContext httpContext, IGroupTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<GroupTypeModel, GroupTypeResponse, IGroupTypeService>(id, httpContext, service, cancellationToken);

    public static Task<IResult> List(HttpContext httpContext, IGroupTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.List<GroupTypeModel, GroupTypeResponse, IGroupTypeService>(httpContext, service, cancellationToken);

    public static async Task<IResult> Update(GroupTypeUpdateRequest request, HttpContext httpContext, IGroupTypeService service, CancellationToken cancellationToken)
    {
        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null) return Results.Unauthorized();
        var stamped = request with { updated_at = DateTimeOffset.UtcNow, updated_by = actor };
        return await BaseResolvers.Update<GroupTypeModel, GroupTypeResponse, GroupTypeUpdateRequest, IGroupTypeService>(stamped, httpContext, service, cancellationToken);
    }


    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IGroupTypeService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<GroupTypeModel, GroupTypeResponse, IGroupTypeService>(
            id, httpContext, service, cancellationToken);

    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IGroupTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<GroupTypeModel, GroupTypeResponse, IGroupTypeService>(id, httpContext, service, cancellationToken);
}

public static class GroupEndpointExtensions
{
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/GroupResponse").WithTags("Group");

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
                .WithDescription("""
                    GET /create. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/create-form", GroupResolvers.CreateForm)
            .WithName("GroupCreateForm")
                .WithDescription("""
                    POST /create-form. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/link/{id:guid}", GroupResolvers.Link)
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

        group.MapGet("/view/{id:guid}", GroupResolvers.Read)
            .WithName("GroupRead")
            .ApplyViewModel<GroupViewPage, GroupResponse>()
                .WithDescription("""
                    GET /view/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/edit/{id:guid}", GroupResolvers.Read)
            .WithName("GroupEdit")
            .ApplyViewModel<GroupEditPage, GroupResponse>()
                .WithDescription("""
                    GET /edit/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/list", GroupResolvers.List)
            .WithName("GroupList")
            .ApplyViewModel<GroupViewListPage, GroupResponse[]>()
                .WithDescription("""
                    Lists entities through the secured IQueryable. Optional `where` is a Hasura-like JSON filter, `take` limits the result to 1..200 rows, and `operation` selects the RBAC operation used to build the secured query (default: list). This supports incremental entity selectors without introducing a separate lookup API.
                    """);

        group.MapGet("/roles/{id:guid}", GroupResolvers.Roles)
            .WithName("GroupRoles")
            .ApplyViewModel<GroupRolesPage, EntityRelationsViewModel<GroupResponse, RoleResponse>>()
                .WithDescription("""
                    GET /roles/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/memberships/{id:guid}", GroupResolvers.Memberships)
            .WithName("GroupMemberships")
            .ApplyViewModel<GroupMembershipsPage, EntityRelationsViewModel<GroupResponse, MembershipResponse>>()
                .WithDescription("""
                    GET /memberships/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPut("/update", GroupResolvers.Update)
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

        group.MapDelete("/delete", GroupResolvers.Delete)
            .WithName("GroupDelete")
                .WithDescription("""
                    DELETE /delete. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        var typeGroup = endpoints.MapGroup(prefix + "/GroupTypeResponse").WithTags("GroupType");
        typeGroup.MapGet("/link/{id:guid}", GroupTypeResolvers.Link)
            .WithName("GroupTypeLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>()
                .WithDescription("""
                    GET /link/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);
        typeGroup.MapGet("/read/{id:guid}", GroupTypeResolvers.Read).WithName("GroupTypeRead").ApplyJsonModel<GroupTypeResponse>()
            .WithDescription("""
                GET /read/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                """);
        typeGroup.MapGet("/list", GroupTypeResolvers.List).WithName("GroupTypeList").ApplyJsonModel<GroupTypeResponse[]>()
            .WithDescription("""
                Lists entities through the secured IQueryable. Optional `where` is a Hasura-like JSON filter, `take` limits the result to 1..200 rows, and `operation` selects the RBAC operation used to build the secured query (default: list). This supports incremental entity selectors without introducing a separate lookup API.
                """);
        typeGroup.MapPut("/update", GroupTypeResolvers.Update).WithName("GroupTypeUpdate").ApplyJsonModel<GroupTypeResponse>()
            .WithDescription("""
                PUT /update. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                """);
        typeGroup.MapDelete("/delete/{id:guid}", GroupTypeResolvers.Delete).WithName("GroupTypeDelete")
            .WithDescription("""
                DELETE /delete/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                """);

        return endpoints;
    }
}
