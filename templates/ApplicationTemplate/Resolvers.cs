using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Endpoints;
namespace RootNamespace.AppName;

public static class AppNameResolvers
{
    public static IEndpointRouteBuilder MapFirstEntityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/appname/FirstEntityResponse").WithTags("AppName.First");
        group.MapGet("/view/{id:guid}", (Guid id,HttpContext http,IFirstEntityService svc,CancellationToken ct) =>
            BaseResolvers.Read<FirstEntityModel,FirstEntityResponse,IFirstEntityService>(id,http,svc,ct));
        group.MapGet("/list", (HttpContext http,IFirstEntityService svc,CancellationToken ct) =>
            BaseResolvers.List<FirstEntityModel,FirstEntityResponse,IFirstEntityService>(http,svc,ct));
        group.MapPut("/update", (FirstEntityUpdateRequest req,HttpContext http,IFirstEntityService svc,CancellationToken ct) =>
            BaseResolvers.Update<FirstEntityModel,FirstEntityResponse,FirstEntityUpdateRequest,IFirstEntityService>(req,http,svc,ct));
        group.MapDelete("/delete/{id:guid}", (Guid id,HttpContext http,IFirstEntityService svc,CancellationToken ct) =>
            BaseResolvers.Delete<FirstEntityModel,FirstEntityResponse,IFirstEntityService>(id,http,svc,ct));
        return endpoints;
    }
    public static IEndpointRouteBuilder MapSecondEntityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/appname/SecondEntityResponse").WithTags("AppName.Second");
        group.MapGet("/view/{id:guid}", (Guid id,HttpContext http,ISecondEntityService svc,CancellationToken ct) =>
            BaseResolvers.Read<SecondEntityModel,SecondEntityResponse,ISecondEntityService>(id,http,svc,ct));
        group.MapGet("/list", (HttpContext http,ISecondEntityService svc,CancellationToken ct) =>
            BaseResolvers.List<SecondEntityModel,SecondEntityResponse,ISecondEntityService>(http,svc,ct));
        group.MapPut("/update", (SecondEntityUpdateRequest req,HttpContext http,ISecondEntityService svc,CancellationToken ct) =>
            BaseResolvers.Update<SecondEntityModel,SecondEntityResponse,SecondEntityUpdateRequest,ISecondEntityService>(req,http,svc,ct));
        group.MapDelete("/delete/{id:guid}", (Guid id,HttpContext http,ISecondEntityService svc,CancellationToken ct) =>
            BaseResolvers.Delete<SecondEntityModel,SecondEntityResponse,ISecondEntityService>(id,http,svc,ct));
        return endpoints;
    }
}
