using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Resolvers;

public static class GroupResolvers
{
    public static async Task<IResult> CreatePage(
        HttpContext httpContext,
        IRbacService rbacService,
        ICurrentActorProvider actorProvider,
        CancellationToken cancellationToken)
    {
        if (actorProvider.TryGetActorUserId(httpContext) is null)
            return Results.Unauthorized();

        var allowed = await rbacService.IsCurrentActorAllowedAsync(
            httpContext,
            RbacWellKnown.RootSecurityObjectId,
            RbacOperations.Create,
            cancellationToken);

        if (!allowed)
            return Results.Forbid();

        return new RazorComponentResult<GroupCreatePage>();
    }

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

    public static async Task<IResult> CreateForm(
        HttpContext httpContext,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var name = form["Name"].ToString();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Name is required." });

        var result = await Create(new GroupCreateRequest(name), httpContext, service, cancellationToken);
        if (result is IModelResult modelResult && modelResult.Model is GroupResponse created)
            return Results.Redirect($"/architecture/GroupResponse/edit/{created.id}");

        return result;
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
            DateTimeOffset.UtcNow,
            actorUserId);

        var updated = await service.UpdateAsync(request, cancellationToken);
        return ApiResults.Ok(updated);
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

    public static async Task<IResult> Create(GroupTypeCreateRequest request, HttpContext httpContext, IGroupTypeService service, CancellationToken cancellationToken)
    {
        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null) return Results.Unauthorized();
        var now = DateTimeOffset.UtcNow;
        var stamped = request with { created_at = now, updated_at = now, created_by = actor, updated_by = actor };
        return await BaseResolvers.Create<GroupTypeModel, GroupTypeResponse, GroupTypeCreateRequest, IGroupTypeService>(stamped, httpContext, service, cancellationToken);
    }

    public static async Task<IResult> Update(GroupTypeUpdateRequest request, HttpContext httpContext, IGroupTypeService service, CancellationToken cancellationToken)
    {
        var actorProvider = httpContext.RequestServices.GetRequiredService<ICurrentActorProvider>();
        var actor = actorProvider.TryGetActorUserId(httpContext);
        if (actor is null) return Results.Unauthorized();
        var stamped = request with { updated_at = DateTimeOffset.UtcNow, updated_by = actor };
        return await BaseResolvers.Update<GroupTypeModel, GroupTypeResponse, GroupTypeUpdateRequest, IGroupTypeService>(stamped, httpContext, service, cancellationToken);
    }

    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IGroupTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<GroupTypeModel, GroupTypeResponse, IGroupTypeService>(id, httpContext, service, cancellationToken);
}

public static class GroupEndpointExtensions
{
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/GroupResponse").WithTags("Group");

        group.MapPost("/create", GroupResolvers.Create)
            .WithName("GroupCreate")
            .ApplyJsonModel<GroupResponse>();

        group.MapGet("/create", GroupResolvers.CreatePage)
            .WithName("GroupCreatePage");

        group.MapPost("/create-form", GroupResolvers.CreateForm)
            .WithName("GroupCreateForm");

        group.MapGet("/view/{id:guid}", GroupResolvers.Read)
            .WithName("GroupRead")
            .ApplyViewModel<GroupViewPage, GroupResponse>();

        group.MapGet("/edit/{id:guid}", GroupResolvers.Read)
            .WithName("GroupEdit")
            .ApplyViewModel<GroupEditPage, GroupResponse>();

        group.MapGet("/list", GroupResolvers.List)
            .WithName("GroupList")
            .ApplyViewModel<GroupViewListPage, GroupResponse[]>();

        group.MapGet("/roles/{id:guid}", GroupResolvers.Roles)
            .WithName("GroupRoles")
            .ApplyViewModel<GroupRolesPage, EntityRelationsViewModel<GroupResponse, RoleResponse>>();

        group.MapGet("/memberships/{id:guid}", GroupResolvers.Memberships)
            .WithName("GroupMemberships")
            .ApplyViewModel<GroupMembershipsPage, EntityRelationsViewModel<GroupResponse, MembershipResponse>>();

        group.MapPut("/update", GroupResolvers.Update)
            .WithName("GroupUpdate")
            .ApplyJsonModel<GroupResponse>();

        group.MapPost("/update/{id:guid}", GroupResolvers.UpdateForm)
            .WithName("GroupUpdateForm")
            .ApplyViewModel<GroupEditPage, GroupResponse>();

        group.MapDelete("/delete", GroupResolvers.Delete)
            .WithName("GroupDelete");

        var typeGroup = endpoints.MapGroup(prefix + "/GroupTypeResponse").WithTags("GroupType");
        typeGroup.MapPost("/create", GroupTypeResolvers.Create).WithName("GroupTypeCreate").ApplyJsonModel<GroupTypeResponse>();
        typeGroup.MapGet("/read/{id:guid}", GroupTypeResolvers.Read).WithName("GroupTypeRead").ApplyJsonModel<GroupTypeResponse>();
        typeGroup.MapGet("/list", GroupTypeResolvers.List).WithName("GroupTypeList").ApplyJsonModel<GroupTypeResponse[]>();
        typeGroup.MapPut("/update", GroupTypeResolvers.Update).WithName("GroupTypeUpdate").ApplyJsonModel<GroupTypeResponse>();
        typeGroup.MapDelete("/delete/{id:guid}", GroupTypeResolvers.Delete).WithName("GroupTypeDelete");

        return endpoints;
    }
}
