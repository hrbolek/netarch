using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.RoleType.Models;
using WebAppArchitecture.RoleType.Services;
using WebAppArchitecture.RoleType.UI.Pages;

namespace WebAppArchitecture.RoleType.Resolvers;

public static class RoleTypeResolvers
{
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

        group.MapDelete("/delete", RoleTypeResolvers.Delete)
            .WithName("RoleTypeDelete");

        return endpoints;
    }
}
