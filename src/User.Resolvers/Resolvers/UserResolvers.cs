using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.UI.Pages;

using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Services;
using WebAppArchitecture.User.UI.Pages;

namespace WebAppArchitecture.User.Resolvers;

public static class UserResolvers
{
    public static async Task<IResult> Create(
        UserCreateRequest request,
        HttpContext httpContext,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actorUserId = actorProvider.TryGetActorUserId(httpContext);
        if (actorUserId is null)
            return Results.Unauthorized();

        var now = DateTimeOffset.UtcNow;
        var stampedRequest = request with
        {
            created_at = now,
            updated_at = now,
            created_by = actorUserId,
            updated_by = actorUserId
        };

        return await BaseResolvers.Create<UserModel, UserResponse, UserCreateRequest, IUserService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
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
            var authService = httpContext.RequestServices.GetRequiredService<IRbacAuthorizationService>();
            var actorUserId = actorProvider.TryGetActorUserId(httpContext);
            if (actorUserId is null)
                return Results.Unauthorized();

            var predicate = HasuraFilterExpression.Build<UserModel>(request.Where ?? new Dictionary<string, object?>());
            var models = await service.VectorAsync(predicate, cancellationToken);

            var filtered = new List<UserResponse>();
            foreach (var model in models)
            {
                var allowed = await authService.IsAllowedAsync(
                    actorUserId.Value,
                    model.rbac_id,
                    RbacOperations.Vector,
                    cancellationToken);

                if (allowed)
                    filtered.Add(model);
            }

            return ApiResults.Ok(filtered.ToArray());
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

        var authorization = await BaseResolvers.AuthorizeModelAsync(
            httpContext,
            user,
            RbacOperations.Read,
            cancellationToken);

        if (authorization is not null)
            return authorization;

        var memberships = await membershipService.VectorAsync(membership => membership.user_id == id, cancellationToken);
        var filteredMemberships = await BaseResolvers.FilterAuthorizedAsync(
            httpContext,
            memberships,
            RbacOperations.List,
            cancellationToken);

        return ApiResults.Ok(new EntityRelationsViewModel<UserResponse, MembershipResponse>(
            user,
            filteredMemberships,
            "Uživatel: členství",
            "Memberships"));
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

        var authorization = await BaseResolvers.AuthorizeModelAsync(
            httpContext,
            user,
            RbacOperations.Read,
            cancellationToken);

        if (authorization is not null)
            return authorization;

        var roles = await roleService.VectorAsync(role => role.user_id == id, cancellationToken);
        var filteredRoles = await BaseResolvers.FilterAuthorizedAsync(
            httpContext,
            roles,
            RbacOperations.List,
            cancellationToken);

        return ApiResults.Ok(new EntityRelationsViewModel<UserResponse, RoleResponse>(
            user,
            filteredRoles,
            "Uživatel: role",
            "Roles"));
    }
}

public sealed record UserVectorRequest(
    Dictionary<string, object?>? Where);

public static class UserEndpointExtensions
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix + "/UserResponse")
            .WithTags("User");

        group.MapPost("/create", UserResolvers.Create)
            .WithName("UserCreate")
            .ApplyJsonModel<UserResponse>();

        group.MapGet("/view/{id:guid}", UserResolvers.Read)
            .WithName("UserRead")
            .ApplyViewModel<UserViewPage, UserResponse>();

        group.MapGet("/edit/{id:guid}", UserResolvers.Read)
            .WithName("UserEdit")
            .ApplyViewModel<UserEditPage, UserResponse>();

        group.MapGet("/list", UserResolvers.List)
            .WithName("UserList")
            .ApplyViewModel<UserViewListPage, UserResponse[]>();

        group.MapGet("/{id:guid}/memberships", UserResolvers.Memberships)
            .WithName("UserMemberships")
            .ApplyViewModel<UserMembershipsPage, EntityRelationsViewModel<UserResponse, MembershipResponse>>();

        group.MapGet("/{id:guid}/roles", UserResolvers.Roles)
            .WithName("UserRoles")
            .ApplyViewModel<UserRolesPage, EntityRelationsViewModel<UserResponse, RoleResponse>>();

        // Structured Hasura-like filters are sent as JSON in the request body.
        group.MapPost("/vector", UserResolvers.Vector)
            .WithName("UserVector")
            .ApplyJsonModel<UserResponse[]>();

        group.MapPut("/update", UserResolvers.Update)
            .WithName("UserUpdate")
            .ApplyJsonModel<UserResponse>();

        group.MapDelete("/delete", UserResolvers.Delete)
            .WithName("UserDelete");

        return endpoints;
    }
}
