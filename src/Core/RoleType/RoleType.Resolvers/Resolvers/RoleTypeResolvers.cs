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
using WebAppArchitecture.RoleType.Models;
using WebAppArchitecture.RoleType.Services;
using WebAppArchitecture.RoleType.UI.Pages;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.RoleType.Resolvers;

public static class RoleTypeResolvers
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

        return new RazorComponentResult<RoleTypeCreatePage>();
    }

    public static async Task<IResult> Create(RoleTypeCreateRequest request, HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
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

        return await BaseResolvers.Create<RoleTypeModel, RoleTypeResponse, RoleTypeCreateRequest, IRoleTypeService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> CreateForm(
        HttpContext httpContext,
        IRoleTypeService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var name = form["name"].ToString();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "name is required." });

        var operations = (form["operations"].ToString() ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var result = await Create(new RoleTypeCreateRequest(name, operations), httpContext, service, cancellationToken);
        if (result is IModelResult modelResult && modelResult.Model is RoleTypeResponse created)
            return Results.Redirect($"/architecture/RoleTypeResponse/edit/{created.id}");

        return result;
    }

    public static Task<IResult> Read(Guid id, HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.Read<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(id, httpContext, service, cancellationToken);

    public static Task<IResult> List(HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.List<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(httpContext, service, cancellationToken);

    public static async Task<IResult> Update(RoleTypeUpdateRequest request, HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
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

        return await BaseResolvers.Update<RoleTypeModel, RoleTypeResponse, RoleTypeUpdateRequest, IRoleTypeService>(
            stampedRequest,
            httpContext,
            service,
            cancellationToken);
    }

    public static async Task<IResult> UpdateForm(
        Guid id,
        HttpContext httpContext,
        IRoleTypeService service,
        CancellationToken cancellationToken)
    {
        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var name = form["name"].ToString();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "name is required." });

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

        var request = new RoleTypeUpdateRequest(
            id,
            name,
            existing.operations,
            DateTimeOffset.UtcNow,
            actorUserId);

        var updated = await service.UpdateAsync(request, cancellationToken);
        return ApiResults.Ok(updated);
    }


    public static Task<IResult> Link(
        Guid id,
        HttpContext httpContext,
        IRoleTypeService service,
        CancellationToken cancellationToken)
        => BaseResolvers.Link<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(
            id, httpContext, service, cancellationToken);

    public static Task<IResult> Delete(Guid id, HttpContext httpContext, IRoleTypeService service, CancellationToken cancellationToken)
        => BaseResolvers.Delete<RoleTypeModel, RoleTypeResponse, IRoleTypeService>(id, httpContext, service, cancellationToken);
}

public static class RoleTypeEndpointExtensions
{
    public static IEndpointRouteBuilder MapRoleTypeEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix + "/RoleTypeResponse").WithTags("RoleType");

        group.MapPost("/create", RoleTypeResolvers.Create)
            .WithName("RoleTypeCreate")
            .ApplyJsonModel<RoleTypeResponse>();

        group.MapGet("/create", RoleTypeResolvers.CreatePage)
            .WithName("RoleTypeCreatePage");

        group.MapPost("/create-form", RoleTypeResolvers.CreateForm)
            .WithName("RoleTypeCreateForm");


        group.MapGet("/link/{id:guid}", RoleTypeResolvers.Link)
            .WithName("RoleTypeLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>();

        group.MapGet("/view/{id:guid}", RoleTypeResolvers.Read)
            .WithName("RoleTypeRead")
            .ApplyViewModel<RoleTypeViewPage, RoleTypeResponse>();

        group.MapGet("/edit/{id:guid}", RoleTypeResolvers.Read)
            .WithName("RoleTypeEdit")
            .ApplyViewModel<RoleTypeEditPage, RoleTypeResponse>();

        group.MapGet("/list", RoleTypeResolvers.List)
            .WithName("RoleTypeList")
            .ApplyViewModel<RoleTypeViewListPage, RoleTypeResponse[]>();

        group.MapPut("/update", RoleTypeResolvers.Update)
            .WithName("RoleTypeUpdate")
            .ApplyJsonModel<RoleTypeResponse>();

        group.MapPost("/update/{id:guid}", RoleTypeResolvers.UpdateForm)
            .WithName("RoleTypeUpdateForm")
            .ApplyViewModel<RoleTypeEditPage, RoleTypeResponse>();

        group.MapDelete("/delete", RoleTypeResolvers.Delete)
            .WithName("RoleTypeDelete");

        return endpoints;
    }
}
