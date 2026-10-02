using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Base.Services;

public interface IBaseService<TDBModel, TDTOModel>
    where TDBModel : IEntityDBModel
    where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
{
    Task<TDTOModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TDTOModel>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TDTOModel> CreateAsync<TCreateRequest>(
        TCreateRequest model,
        CancellationToken cancellationToken = default)
        where TCreateRequest : ICreateDTOModel<TDBModel>;

    Task<TDTOModel> UpdateAsync<TUpdateRequest>(
        TUpdateRequest model,
        CancellationToken cancellationToken = default)
        where TUpdateRequest : IUpdateDTOModel<TDBModel>, IEntityDTOModel<TDBModel, TUpdateRequest>;

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

public class BaseService<TDBModel, TDTOModel, TStore>
    : IBaseService<TDBModel, TDTOModel>
    where TDBModel : IEntityDBModel
    where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
    where TStore : IBaseStore<TDBModel>
{
    protected readonly TStore Store;

    public BaseService(TStore store)
        => Store = store;

    public virtual async Task<TDTOModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var model = await Store.GetByIdAsync(
            id,
            cancellationToken);

        return model is null
            ? default
            : TDTOModel.FromModel(model);
    }

    public virtual async Task<IReadOnlyCollection<TDTOModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var models = await Store.GetAllAsync(
            cancellationToken);

        return models
            .Select(TDTOModel.FromModel)
            .ToArray();
    }

    public virtual async Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var models = await Store.VectorAsync(
            predicate,
            cancellationToken);

        return models
            .Select(TDTOModel.FromModel)
            .ToArray();
    }

    public virtual async Task<TDTOModel> CreateAsync<TCreateRequest>(
        TCreateRequest model,
        CancellationToken cancellationToken = default)
        where TCreateRequest : ICreateDTOModel<TDBModel>
    {
        var dbModel = model.ToModel();

        var created = await Store.CreateAsync(
            dbModel,
            cancellationToken);

        return TDTOModel.FromModel(created);
    }

    public virtual async Task<TDTOModel> UpdateAsync<TUpdateRequest>(
        TUpdateRequest model,
        CancellationToken cancellationToken = default)
        where TUpdateRequest : IUpdateDTOModel<TDBModel>, IEntityDTOModel<TDBModel, TUpdateRequest>
    {
        var current = await Store.GetByIdAsync(
            model.id,
            cancellationToken);

        if (current is null)
        {
            throw new InvalidOperationException(
                $"Item with id '{model.id}' does not exist.");
        }

        var updatedModel = model.ApplyTo(current);

        var updated = await Store.UpdateAsync(
            updatedModel,
            cancellationToken);

        return TDTOModel.FromModel(updated);
    }

    public virtual Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => Store.DeleteAsync(
            id,
            cancellationToken);
}