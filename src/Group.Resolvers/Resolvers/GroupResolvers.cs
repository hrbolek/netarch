using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Group.Models;
using WebAppArchitecture.Group.Services;
using WebAppArchitecture.Group.UI.Pages;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Services;

namespace WebAppArchitecture.Group.Resolvers;

public static class GroupResolvers
{
    public static async Task<IResult> Create(
        GroupCreateRequest request,
        HttpContext httpContext,
        IGroupService service,
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

        return await BaseResolvers.Create<GroupModel, GroupResponse, GroupCreateRequest, IGroupService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
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

    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IGroupService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<GroupModel, GroupResponse, IGroupService>(id, httpContext, service, cancellationToken);

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

        var authorization = await BaseResolvers.AuthorizeModelAsync(
            httpContext,
            group,
            RbacOperations.Read,
            cancellationToken);

        if (authorization is not null)
            return authorization;

        var roles = await roleService.VectorAsync(role => role.group_id == id, cancellationToken);
        var filteredRoles = await BaseResolvers.FilterAuthorizedAsync(
            httpContext,
            roles,
            RbacOperations.List,
            cancellationToken);

        return ApiResults.Ok(new EntityRelationsViewModel<GroupResponse, RoleResponse>(
            group,
            filteredRoles,
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

        var authorization = await BaseResolvers.AuthorizeModelAsync(
            httpContext,
            group,
            RbacOperations.Read,
            cancellationToken);

        if (authorization is not null)
            return authorization;

        var memberships = await membershipService.VectorAsync(membership => membership.group_id == id, cancellationToken);
        var filteredMemberships = await BaseResolvers.FilterAuthorizedAsync(
            httpContext,
            memberships,
            RbacOperations.List,
            cancellationToken);

        return ApiResults.Ok(new EntityRelationsViewModel<GroupResponse, MembershipResponse>(
            group,
            filteredMemberships,
            "Skupina: členství",
            "Memberships"));
    }
}

public static class GroupEndpointExtensions
{
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/GroupResponse").WithTags("Group");

        group.MapPost("/create", GroupResolvers.Create)
            .WithName("GroupCreate")
            .ApplyJsonModel<GroupResponse>();

        group.MapGet("/view/{id:guid}", GroupResolvers.Read)
            .WithName("GroupRead")
            .ApplyViewModel<GroupViewPage, GroupResponse>();

        group.MapGet("/edit/{id:guid}", GroupResolvers.Read)
            .WithName("GroupEdit")
            .ApplyViewModel<GroupEditPage, GroupResponse>();

        group.MapGet("/list", GroupResolvers.List)
            .WithName("GroupList")
            .ApplyViewModel<GroupViewListPage, GroupResponse[]>();

        group.MapGet("/{id:guid}/roles", GroupResolvers.Roles)
            .WithName("GroupRoles")
            .ApplyViewModel<GroupRolesPage, EntityRelationsViewModel<GroupResponse, RoleResponse>>();

        group.MapGet("/{id:guid}/memberships", GroupResolvers.Memberships)
            .WithName("GroupMemberships")
            .ApplyViewModel<GroupMembershipsPage, EntityRelationsViewModel<GroupResponse, MembershipResponse>>();

        group.MapPut("/update", GroupResolvers.Update)
            .WithName("GroupUpdate")
            .ApplyJsonModel<GroupResponse>();

        group.MapDelete("/delete", GroupResolvers.Delete)
            .WithName("GroupDelete");

        return endpoints;
    }
}
