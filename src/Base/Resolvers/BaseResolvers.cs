using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Base.Resolvers;
public static class BaseResolvers
{
    public static async Task<IResult?> AuthorizeModelAsync<TDTOModel>(
        HttpContext httpContext,
        TDTOModel model,
        string operation,
        CancellationToken cancellationToken)
    {
        return await AuthorizeSingleAsync(
            httpContext,
            model,
            operation,
            cancellationToken);
    }

    public static async Task<TDTOModel[]> FilterAuthorizedAsync<TDTOModel>(
        HttpContext httpContext,
        IEnumerable<TDTOModel> models,
        string operation,
        CancellationToken cancellationToken)
    {
        var authContext = ResolveAuthorizationContext(httpContext);
        if (authContext is null)
            return models.ToArray();

        if (authContext.ActorUserId is null)
            return Array.Empty<TDTOModel>();

        var filtered = new List<TDTOModel>();

        foreach (var model in models)
        {
            if (model is not ISecuredEntityDTOModel secured)
            {
                filtered.Add(model);
                continue;
            }

            var allowed = await authContext.AuthorizationService.IsAllowedAsync(
                authContext.ActorUserId.Value,
                secured.rbac_id,
                operation,
                cancellationToken);

            if (allowed)
                filtered.Add(model);
        }

        return filtered.ToArray();
    }

    public static async Task<IResult> Read<
        TDBModel,
        TDTOModel,
        TService>(
        Guid id,
        HttpContext httpContext,
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
        var result = await service.GetByIdAsync(
            id,
            cancellationToken);

        if (result is null)
            return Results.NotFound();

        var authorizeResult = await AuthorizeSingleAsync(
            httpContext,
            result,
            RbacOperations.Read,
            cancellationToken);

        if (authorizeResult is not null)
            return authorizeResult;

        return ApiResults.Ok(result);
    }

    public static async Task<IResult> List<
        TDBModel,
        TDTOModel,
        TService>(
        HttpContext httpContext,
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
        var result = await service.GetAllAsync(
            cancellationToken);

        var filtered = await FilterAuthorizedAsync(
            httpContext,
            result,
            RbacOperations.List,
            cancellationToken);

        if (ResolveAuthorizationContext(httpContext) is not null && filtered.Length == 0)
        {
            var actorProvider = httpContext.RequestServices.GetService<ICurrentActorProvider>();
            if (actorProvider?.TryGetActorUserId(httpContext) is null)
                return Results.Unauthorized();
        }

        return ApiResults.Ok(filtered);
    }

    public static async Task<IResult> Create<
        TDBModel,
        TDTOModel,
        TCreateRequest,
        TService>(
        TCreateRequest request,
        HttpContext httpContext,
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TCreateRequest : ICreateDTOModel<TDBModel>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
        if (request is ISecuredCreateRequest securedCreate)
        {
            var authContext = ResolveAuthorizationContext(httpContext);
            if (authContext is not null)
            {
                if (authContext.ActorUserId is null)
                    return Results.Unauthorized();

                var targetRbacId = securedCreate.rbac_id ?? RbacWellKnown.RootSecurityObjectId;

                var allowed = await authContext.AuthorizationService.IsAllowedAsync(
                    authContext.ActorUserId.Value,
                    targetRbacId,
                    RbacOperations.Create,
                    cancellationToken);

                if (!allowed)
                    return Results.Forbid();
            }
        }

        var result = await service.CreateAsync(
            request,
            cancellationToken);

        return ApiResults.Ok(result);
    }

    public static async Task<IResult> Update<
        TDBModel,
        TDTOModel,
        TUpdateRequest,
        TService>(
        TUpdateRequest request,
        HttpContext httpContext,
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TUpdateRequest :
            IUpdateDTOModel<TDBModel>,
            IEntityDTOModel<TDBModel, TUpdateRequest>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
        var current = await service.GetByIdAsync(
            request.id,
            cancellationToken);

        if (current is null)
            return Results.NotFound();

        var authorizeResult = await AuthorizeSingleAsync(
            httpContext,
            current,
            RbacOperations.Update,
            cancellationToken);

        if (authorizeResult is not null)
            return authorizeResult;

        var result = await service.UpdateAsync(
            request,
            cancellationToken);

        return ApiResults.Ok(result);
    }

    public static async Task<IResult> Delete<
        TDBModel,
        TDTOModel,
        TService>(
        Guid id,
        HttpContext httpContext,
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
        var current = await service.GetByIdAsync(
            id,
            cancellationToken);

        if (current is null)
            return Results.NotFound();

        var authorizeResult = await AuthorizeSingleAsync(
            httpContext,
            current,
            RbacOperations.Delete,
            cancellationToken);

        if (authorizeResult is not null)
            return authorizeResult;

        await service.DeleteAsync(
            id,
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult?> AuthorizeSingleAsync<TDTOModel>(
        HttpContext httpContext,
        TDTOModel model,
        string operation,
        CancellationToken cancellationToken)
    {
        if (model is not ISecuredEntityDTOModel secured)
            return null;

        var authContext = ResolveAuthorizationContext(httpContext);
        if (authContext is null)
            return null;

        if (authContext.ActorUserId is null)
            return Results.Unauthorized();

        var allowed = await authContext.AuthorizationService.IsAllowedAsync(
            authContext.ActorUserId.Value,
            secured.rbac_id,
            operation,
            cancellationToken);

        return allowed
            ? null
            : Results.Forbid();
    }

    private static BaseResolverAuthorizationContext? ResolveAuthorizationContext(HttpContext httpContext)
    {
        var authorizationService = httpContext.RequestServices.GetService<IRbacAuthorizationService>();
        if (authorizationService is null)
            return null;

        var actorProvider = httpContext.RequestServices.GetService<ICurrentActorProvider>();
        if (actorProvider is null)
            return null;

        var actorUserId = actorProvider.TryGetActorUserId(httpContext);
        return new BaseResolverAuthorizationContext(authorizationService, actorUserId);
    }

    private sealed record BaseResolverAuthorizationContext(
        IRbacAuthorizationService AuthorizationService,
        Guid? ActorUserId);
}

// public class BaseResolvers<
//     TService,
//     TDBModel,
//     TDTOModel>
//     where TDBModel : IEntityDBModel
//     where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
//     where TService : IBaseService<TDBModel, TDTOModel>
// {
//     public static async Task<IResult> Create<TCreateRequest>(
//         TCreateRequest request,
//         TService service,
//         CancellationToken cancellationToken)
//         where TCreateRequest : ICreateDTOModel<TDBModel>
//     {
//         var result = await service.CreateAsync(
//             request,
//             cancellationToken);

//         return ApiResults.Ok(result);
//     }

//     public static async Task<IResult> Update<TUpdateRequest>(
//         TUpdateRequest request,
//         TService service,
//         CancellationToken cancellationToken)
//         where TUpdateRequest : IUpdateDTOModel<TDBModel>
//     {
//         var result = await service.UpdateAsync(
//             request,
//             cancellationToken);

//         return ApiResults.Ok(result);
//     }
// }