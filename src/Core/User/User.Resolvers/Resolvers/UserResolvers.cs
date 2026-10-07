using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Components;

using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Services;
using WebAppArchitecture.User.UI.Pages;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Group.Services;

namespace WebAppArchitecture.User.Resolvers;

public static class UserResolvers
{
    public static IResult CreatePage(ICurrentActorProvider actorProvider, HttpContext httpContext)
    {
        if (actorProvider.TryGetActorUserId(httpContext) is null)
            return Results.Unauthorized();
        return new RazorComponentResult<UserCreatePage>();
    }

    public static async Task<IResult> CreateContained(
        UserCreateContainedRequest request,
        IUserService service,
        CancellationToken cancellationToken)
        => ApiResults.Ok(await service.CreateContainedAsync(request, cancellationToken));

    public static async Task<IResult> CreateForm(
        HttpContext httpContext,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var name = form["Name"].ToString();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Name is required." });
        if (!Guid.TryParse(form["group_id"].ToString(), out var groupId))
            return Results.BadRequest(new { error = "group_id must be a valid Guid." });

        var created = await service.CreateContainedAsync(new UserCreateContainedRequest(
            name, groupId, form["email"].ToString(), form["surname"].ToString()), cancellationToken);
        return Results.Redirect($"/architecture/UserResponse/edit/{created.id}");
    }

    public static async Task<IResult> Read(
        Guid id,
        HttpContext httpContext,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.Read<UserModel, UserResponse, IUserService>(
            id,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> List(
        HttpContext httpContext,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.List<UserModel, UserResponse, IUserService>(
            httpContext,
            service,
            cancellationToken);
    }

    /// <summary>
    /// Accepts a Hasura-like dictionary, translates it to a typed predicate
    /// and delegates the actual filtering to the service/store layer.
    /// </summary>
    public static async Task<IResult> Vector(
        UserVectorRequest request,
        HttpContext httpContext,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
            var actorUserId = actorProvider.TryGetActorUserId(httpContext);
            if (actorUserId is null)
                return Results.Unauthorized();

            var predicate = HasuraFilterExpression.Build<UserModel>(request.Where ?? new Dictionary<string, object?>());
            var models = await service.VectorAsync(
                predicate,
                RbacOperations.Vector,
                cancellationToken);

            return ApiResults.Ok(models);
        }
        catch (HasuraFilterException ex)
        {
            return Results.BadRequest(new
            {
                error = "Invalid filter.",
                detail = ex.Message
            });
        }
    }

    public static async Task<IResult> Update(
        UserUpdateRequest request,
        HttpContext httpContext,
        IUserService service,
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

        return await BaseResolvers.Update<UserModel, UserResponse, UserUpdateRequest, IUserService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> UpdateForm(
        Guid id,
        HttpContext httpContext,
        IUserService service,
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

        var request = new UserUpdateRequest(
            id,
            name,
            DateTimeOffset.UtcNow,
            actorUserId);

        var updated = await service.UpdateAsync(request, cancellationToken);
        return ApiResults.Ok(updated);
    }


    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IUserService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<UserModel, UserResponse, IUserService>(
            id, httpContext, service, cancellationToken);

    public static async Task<IResult> Delete(
        Guid id,
        HttpContext httpContext,
        IUserService service,
        CancellationToken cancellationToken)
    {
        return await BaseResolvers.Delete<UserModel, UserResponse, IUserService>(
            id,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> Memberships(
        Guid id,
        HttpContext httpContext,
        IUserService userService,
        IMembershipService membershipService,
        CancellationToken cancellationToken)
    {
        var user = await userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return Results.NotFound();

        var memberships = await membershipService.VectorAsync(membership => membership.user_id == id, cancellationToken);

        return ApiResults.Ok(new EntityRelationsViewModel<UserResponse, MembershipResponse>(
            user,
            memberships,
            "Uživatel: členství",
            "Memberships"));
    }

    public static async Task<IResult> Actions(
        Guid id,
        HttpContext httpContext,
        IUserService userService,
        IGroupService groupService,
        ICurrentActorProvider actorProvider,
        IRbacAuthorizationService authorizationService,
        CancellationToken cancellationToken)
    {
        var user = await userService.GetByIdAsync(id, cancellationToken);
        if (user is null) return Results.NotFound();

        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null) return Results.Unauthorized();

        var canUpdate = await authorizationService.IsAllowedAsync(actor.Value, user.rbacobject_id, RbacOperations.Update, cancellationToken);
        var canDelete = await authorizationService.IsAllowedAsync(actor.Value, user.rbacobject_id, RbacOperations.Delete, cancellationToken);
        var targetGroups = await groupService.VectorAsync(_ => true, RbacOperations.Create, 1, cancellationToken);
        var canCreateRelation = targetGroups.Count > 0;
        var returnUrl = $"/architecture/UserResponse/view/{id}";

        var actions = new EntityAction[]
        {
            new("view", "View", returnUrl, EntityActionKind.Navigate, Group: "navigation", Order: 10),
            new("edit", "Edit", $"/architecture/UserResponse/edit/{id}", EntityActionKind.Update, canUpdate,
                canUpdate ? null : "You do not have update permission for this user.", "navigation", 20),
            new("add-to-group", "Add to group", $"/architecture/MembershipResponse/create?user_id={id}&return_url={Uri.EscapeDataString(returnUrl)}",
                EntityActionKind.Relation, canCreateRelation, canCreateRelation ? null : "You cannot create membership in any available group.", "relations", 30),
            new("assign-role", "Assign role", $"/architecture/RoleResponse/create?user_id={id}&return_url={Uri.EscapeDataString(returnUrl)}",
                EntityActionKind.Relation, canCreateRelation, canCreateRelation ? null : "You cannot assign a role in any available group.", "relations", 40),
            new("delete", "Delete", $"/architecture/UserResponse/delete?id={id}", EntityActionKind.Delete, canDelete,
                canDelete ? null : "You do not have delete permission for this user.", "danger", 90, "DELETE",
                "Delete this user?", "/architecture/UserResponse/list")
        };

        return ApiResults.Ok(new EntityActionMenuModel(actions));
    }

    public static async Task<IResult> Roles(
        Guid id,
        HttpContext httpContext,
        IUserService userService,
        IRoleService roleService,
        CancellationToken cancellationToken)
    {
        var user = await userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return Results.NotFound();

        var roles = await roleService.VectorAsync(role => role.user_id == id, cancellationToken);

        return ApiResults.Ok(new EntityRelationsViewModel<UserResponse, RoleResponse>(
            user,
            roles,
            "Uživatel: role",
            "Roles"));
    }
}

public sealed record UserVectorRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("where")]
    Dictionary<string, object?>? Where);

public static class UserEndpointExtensions
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix + "/UserResponse")
            .WithTags("User");

        group.MapPost("/create-contained", UserResolvers.CreateContained)
            .WithName("UserCreateContained")
            .ApplyJsonModel<UserResponse>()
                .WithDescription("""
                    Creates a user inside group_id and immediately creates its Membership. Create permission is evaluated on the target Group; no client-supplied RBAC object is accepted.
                    """);

        group.MapGet("/create", UserResolvers.CreatePage)
            .WithName("UserCreatePage")
                .WithDescription("""
                    GET /create. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapPost("/create-form", UserResolvers.CreateForm)
            .WithName("UserCreateForm")
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
            .ApplyViewModel<UserMembershipsPage, EntityRelationsViewModel<UserResponse, MembershipResponse>>()
                .WithDescription("""
                    GET /memberships/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                    """);

        group.MapGet("/roles/{id:guid}", UserResolvers.Roles)
            .WithName("UserRoles")
            .ApplyViewModel<UserRolesPage, EntityRelationsViewModel<UserResponse, RoleResponse>>()
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
