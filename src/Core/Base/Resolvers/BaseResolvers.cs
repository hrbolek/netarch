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
        var where = httpContext.Request.Query["where"].ToString();

        if (string.IsNullOrWhiteSpace(where))
        {
            var all = await service.GetAllAsync(cancellationToken);
            return ApiResults.Ok(all);
        }

        try
        {
            var filter = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(where)
                ?? new Dictionary<string, object?>();
            var predicate = HasuraFilterExpression.Build<TDBModel>(filter);
            var result = await service.VectorAsync(predicate, RbacOperations.List, cancellationToken);
            return ApiResults.Ok(result);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return Results.BadRequest(new { error = "The 'where' query parameter must be a valid JSON object.", detail = ex.Message });
        }
        catch (HasuraFilterException ex)
        {
            return Results.BadRequest(new { error = "Invalid Hasura-like filter expression.", detail = ex.Message });
        }
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

                var targetRbacId = securedCreate.rbacobject_id ?? RbacWellKnown.RootSecurityObjectId;

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

    public static async Task<IResult> Link<
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
        var result = await service.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return Results.NotFound();

        return ApiResults.Ok(new EntityLinkResponse(
            result.id,
            result.visual_label,
            typeof(TDTOModel).Name));
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
            secured.rbacobject_id,
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
