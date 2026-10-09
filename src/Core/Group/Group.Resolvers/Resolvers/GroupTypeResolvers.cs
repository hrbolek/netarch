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
