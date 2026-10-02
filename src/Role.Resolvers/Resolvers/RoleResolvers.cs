using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Role.Models;
using WebAppArchitecture.Role.Services;
using WebAppArchitecture.Role.UI.Pages;

namespace WebAppArchitecture.Role.Resolvers;

public static class RoleResolvers
{
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

        group.MapDelete("/delete", RoleResolvers.Delete)
            .WithName("RoleDelete");

        return endpoints;
    }
}
