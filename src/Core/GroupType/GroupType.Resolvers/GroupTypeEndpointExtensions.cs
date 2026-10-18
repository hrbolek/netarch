using WebAppArchitecture.Base.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.UI.Components;
using WebAppArchitecture.GroupType.Models;
using WebAppArchitecture.GroupType.Services;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.GroupType.Resolvers;

public static class GroupTypeEndpointExtensions
{
    public static IEndpointRouteBuilder MapGroupTypeEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var typeGroup = endpoints.MapGroup(prefix + "/GroupTypeResponse").WithTags("GroupType");
        typeGroup.MapGet("/link/{id:guid}", BaseResolvers.Link<GroupTypeModel, GroupTypeResponse, IGroupTypeService>)
            .WithName("GroupTypeLink")
            .ApplyViewModel<EntityLinkFragment, EntityLinkResponse>()
            .WithDescription("""
                GET /link/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                """);
        typeGroup.MapGet("/read/{id:guid}", BaseResolvers.Read<GroupTypeModel, GroupTypeResponse, IGroupTypeService>).WithName("GroupTypeRead").ApplyJsonModel<GroupTypeResponse>()
            .WithDescription("""
                GET /read/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                """);
        typeGroup.MapGet("/list", BaseResolvers.List<GroupTypeModel, GroupTypeResponse, IGroupTypeService>).WithName("GroupTypeList").ApplyJsonModel<GroupTypeResponse[]>()
            .WithDescription("""
                Lists entities readable by the current actor. Optional `where` is a Hasura-like JSON filter, and `take` limits the result to 1..200 rows.
                """);
        typeGroup.MapPut("/update", BaseResolvers.Update<GroupTypeModel, GroupTypeResponse, GroupTypeUpdateRequest, IGroupTypeService>).WithName("GroupTypeUpdate").ApplyJsonModel<GroupTypeResponse>()
            .WithDescription("""
                PUT /update. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                """);
        typeGroup.MapDelete("/delete/{id:guid}", BaseResolvers.Delete<GroupTypeModel, GroupTypeResponse, IGroupTypeService>).WithName("GroupTypeDelete")
            .WithDescription("""
                DELETE /delete/{id:guid}. This endpoint is part of the application API and applies the authorization and service rules of its resolver.
                """);
        return endpoints;
    }
}
