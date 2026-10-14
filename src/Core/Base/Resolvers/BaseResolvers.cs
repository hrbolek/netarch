using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Base.Resolvers;
/// <summary>
/// HTTP resolver pro Base Resolvers. Překládá požadavky Minimal API na volání služeb a návratové modely; obchodní pravidla a perzistence patří do Services/DBLayer.
/// </summary>
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
        where TDTOModel : IEntityProjection<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel>
    {
        var result = await service.GetByIdAsync<TDTOModel>(
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
        where TDTOModel : IEntityProjection<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel>
    {
        var operation = httpContext.Request.Query["operation"].ToString();
        if (string.IsNullOrWhiteSpace(operation))
            operation = RbacOperations.List;

        try
        {
            _ = RbacOperationIds.FromName(operation);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Results.BadRequest(new { error = "Unknown RBAC operation requested for the list." });
        }

        if (!VectorQueryParser.TryParse<TDBModel>(
            httpContext,
            _ => true,
            out var predicate,
            out var options,
            out var error))
        {
            return Results.BadRequest(new { error });
        }

        var page = await service.VectorPageAsync<TDTOModel>(predicate, operation, options, cancellationToken);
        var pageInfo = VectorQueryParser.WithRequestState(httpContext, page.PageInfo);
        VectorQueryParser.WritePagingHeaders(httpContext, pageInfo);
        return ApiResults.Ok(page.Items);
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
        where TDTOModel : IEntityProjection<TDBModel, TDTOModel>, IEntityMapper<TDBModel, TDTOModel>
        where TUpdateRequest : IUpdateDTOModel<TDBModel>
        where TService : IBaseService<TDBModel>
    {
        var current = await service.GetByIdAsync<TDTOModel>(
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

        var result = await service.UpdateAsync<TUpdateRequest, TDTOModel>(
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
        where TDTOModel : IEntityProjection<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel>
    {
        var current = await service.GetByIdAsync<TDTOModel>(
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
        where TDTOModel : IEntityProjection<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel>
    {
        var result = await service.GetByIdAsync<TDTOModel>(id, cancellationToken);
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

        // Architecture exception: IRbacService is the authorization substrate below secured
        // entity services. This call decides access to an already projected DTO, not DB access;
        // domain reads and writes must still go through the supplied secured entity service.
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
