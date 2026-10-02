using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Endpoints;

namespace WebAppArchitecture.Base.Resolvers;
public static class BaseResolvers
{
    public static async Task<IResult> Read<
        TDBModel,
        TDTOModel,
        TService>(
        Guid id,
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
        var result = await service.GetByIdAsync(
            id,
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : ApiResults.Ok(result);
    }

    public static async Task<IResult> List<
        TDBModel,
        TDTOModel,
        TService>(
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
        var result = await service.GetAllAsync(
            cancellationToken);

        return ApiResults.Ok(result);
    }

    public static async Task<IResult> Create<
        TDBModel,
        TDTOModel,
        TCreateRequest,
        TService>(
        TCreateRequest request,
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TCreateRequest : ICreateDTOModel<TDBModel>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
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
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TUpdateRequest :
            IUpdateDTOModel<TDBModel>,
            IEntityDTOModel<TDBModel, TUpdateRequest>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
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
        TService service,
        CancellationToken cancellationToken)
        where TDBModel : IEntityDBModel
        where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
        where TService : IBaseService<TDBModel, TDTOModel>
    {
        await service.DeleteAsync(
            id,
            cancellationToken);

        return Results.NoContent();
    }
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