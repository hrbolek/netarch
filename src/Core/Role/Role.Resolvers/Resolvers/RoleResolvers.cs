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

public static class RoleResolvers
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

        return new RazorComponentResult<RoleCreatePage>();
    }

    public static async Task<IResult> Create(RoleCreateRequest request, HttpContext httpContext, IRoleService service, CancellationToken cancellationToken)
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

        return await BaseResolvers.Create<RoleModel, RoleResponse, RoleCreateRequest, IRoleService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> CreateForm(
        HttpContext httpContext,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);

        if (!Guid.TryParse(form["group_id"].ToString(), out var groupId))
            return Results.BadRequest(new { error = "Invalid group_id value." });

        if (!Guid.TryParse(form["user_id"].ToString(), out var userId))
            return Results.BadRequest(new { error = "Invalid user_id value." });

        var roleTypeValue = form["role_type_id"].ToString();
        if (!Guid.TryParse(roleTypeValue, out var roleTypeId))
            return Results.BadRequest(new { error = "role_type_id must be a valid Guid." });

        var isDeputy = string.Equals(form["is_deputy"].ToString(), "true", StringComparison.OrdinalIgnoreCase);

        var validFromRaw = form["valid_from"].ToString();
        var validFrom = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(validFromRaw) && !DateTimeOffset.TryParse(validFromRaw, out validFrom))
            return Results.BadRequest(new { error = "Invalid valid_from value." });

        DateTimeOffset? validTo = null;
        var validToRaw = form["valid_to"].ToString();
        if (!string.IsNullOrWhiteSpace(validToRaw))
        {
            if (!DateTimeOffset.TryParse(validToRaw, out var parsedValidTo))
                return Results.BadRequest(new { error = "Invalid valid_to value." });

            validTo = parsedValidTo;
        }

        var result = await Create(
            new RoleCreateRequest(groupId, userId, roleTypeId, isDeputy, validFrom, validTo),
            httpContext,
            service,
            cancellationToken);

        if (result is IModelResult modelResult && modelResult.Model is RoleResponse created)
            return Results.Redirect($"/architecture/RoleResponse/edit/{created.id}");

        return result;
    }

    public static Task<IResult> Read(Guid id, HttpContext httpContext, IRoleService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<RoleModel, RoleResponse, IRoleService>(id, httpContext, service, cancellationToken);

    public static Task<IResult> List(HttpContext httpContext, IRoleService service, CancellationToken cancellationToken)
        => BaseResolvers.List<RoleModel, RoleResponse, IRoleService>(httpContext, service, cancellationToken);

    public static async Task<IResult> Update(RoleUpdateRequest request, HttpContext httpContext, IRoleService service, CancellationToken cancellationToken)
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

        return await BaseResolvers.Update<RoleModel, RoleResponse, RoleUpdateRequest, IRoleService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> UpdateForm(
        Guid id,
        HttpContext httpContext,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var roleTypeValue = form["role_type_id"].ToString();
        _ = Guid.TryParse(roleTypeValue, out var roleTypeId);
        var isDeputyText = form["is_deputy"].ToString();
        var validFromText = form["valid_from"].ToString();
        var validToText = form["valid_to"].ToString();

        if (roleTypeId == Guid.Empty)
            return Results.BadRequest(new { error = "role_type_id must be a valid Guid." });

        if (!bool.TryParse(isDeputyText, out var isDeputy))
            return Results.BadRequest(new { error = "Invalid is_deputy value." });

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

        if (!DateTimeOffset.TryParse(validFromText, out var parsedValidFrom))
            return Results.BadRequest(new { error = "Invalid valid_from value." });

        DateTimeOffset? parsedValidTo = null;
        if (!string.IsNullOrWhiteSpace(validToText))
        {
            if (!DateTimeOffset.TryParse(validToText, out var parsed))
                return Results.BadRequest(new { error = "Invalid valid_to value." });

            parsedValidTo = parsed;
        }

        var request = new RoleUpdateRequest(
            id,
            roleTypeId,
            isDeputy,
            parsedValidFrom,
            parsedValidTo,
            DateTimeOffset.UtcNow,
            actorUserId);

        var updated = await service.UpdateAsync(request, cancellationToken);
        return ApiResults.Ok(updated);
    }


    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IRoleService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<RoleModel, RoleResponse, IRoleService>(
            id, httpContext, service, cancellationToken);

    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IRoleService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<RoleModel, RoleResponse, IRoleService>(id, httpContext, service, cancellationToken);
}

public static class RoleEndpointExtensions
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/RoleResponse").WithTags("Role");

        group.MapPost("/create", RoleResolvers.Create)
            .WithName("RoleCreate")
            .ApplyJsonModel<RoleResponse>();

        group.MapGet("/create", RoleResolvers.CreatePage)
            .WithName("RoleCreatePage");

        group.MapPost("/create-form", RoleResolvers.CreateForm)
            .WithName("RoleCreateForm");


        group.MapGet("/link/{id:guid}", RoleResolvers.Link)
            .WithName("RoleLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>();

        group.MapGet("/view/{id:guid}", RoleResolvers.Read)
            .WithName("RoleRead")
            .ApplyViewModel<RoleViewPage, RoleResponse>();

        group.MapGet("/edit/{id:guid}", RoleResolvers.Read)
            .WithName("RoleEdit")
            .ApplyViewModel<RoleEditPage, RoleResponse>();

        group.MapGet("/list", RoleResolvers.List)
            .WithName("RoleList")
            .ApplyViewModel<RoleViewListPage, RoleResponse[]>();

        group.MapPut("/update", RoleResolvers.Update)
            .WithName("RoleUpdate")
            .ApplyJsonModel<RoleResponse>();

        group.MapPost("/update/{id:guid}", RoleResolvers.UpdateForm)
            .WithName("RoleUpdateForm")
            .ApplyViewModel<RoleEditPage, RoleResponse>();

        group.MapDelete("/delete", RoleResolvers.Delete)
            .WithName("RoleDelete");

        return endpoints;
    }
}
