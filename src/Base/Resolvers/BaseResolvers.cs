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
        var rbacService = httpContext.RequestServices.GetService<IRbacService>();
        if (rbacService is null)
            return models.ToArray();

        var actorProvider = httpContext.RequestServices.GetService<ICurrentActorProvider>();
        if (actorProvider?.TryGetActorUserId(httpContext) is null)
            return Array.Empty<TDTOModel>();

        var filtered = new List<TDTOModel>();

        foreach (var model in models)
        {
            if (model is not ISecuredEntityDTOModel secured)
            {
                filtered.Add(model);
                continue;
            }

            var allowed = await rbacService.IsCurrentActorAllowedAsync(
                httpContext,
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
        where TDBModel : class, ISecuredEntityDBModel
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
        where TDBModel : class, ISecuredEntityDBModel
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

        if (httpContext.RequestServices.GetService<IRbacService>() is not null && filtered.Length == 0)
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
        where TDBModel : class, ISecuredEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TCreateRequest : ICreateDTOModel<TDBModel>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
        if (request is ISecuredCreateRequest securedCreate)
        {
            var rbacService = httpContext.RequestServices.GetService<IRbacService>();
            if (rbacService is not null)
            {
                var actorProvider = httpContext.RequestServices.GetService<ICurrentActorProvider>();
                if (actorProvider?.TryGetActorUserId(httpContext) is null)
                    return Results.Unauthorized();

                var targetRbacId = securedCreate.rbac_id ?? RbacWellKnown.RootSecurityObjectId;

                var allowed = await rbacService.IsCurrentActorAllowedAsync(
                    httpContext,
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
        where TDBModel : class, ISecuredEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TUpdateRequest : IUpdateDTOModel<TDBModel>
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
        where TDBModel : class, ISecuredEntityDBModel
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

        var rbacService = httpContext.RequestServices.GetService<IRbacService>();
        if (rbacService is null)
            return null;

        var actorProvider = httpContext.RequestServices.GetService<ICurrentActorProvider>();
        if (actorProvider?.TryGetActorUserId(httpContext) is null)
            return Results.Unauthorized();

        var allowed = await rbacService.IsCurrentActorAllowedAsync(
            httpContext,
            secured.rbac_id,
            operation,
            cancellationToken);

        return allowed
            ? null
            : Results.Forbid();
    }

}

// public class BaseResolvers<
//     TService,
//     TDBModel,
//     TDTOModel>
//     where TDBModel : class, ISecuredEntityDBModel
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
