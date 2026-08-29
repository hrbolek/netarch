using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Base.Services;

public interface IBaseService<TDBModel, TDTOModel>
    where TDBModel : IEntityDBModel
    where TDTOModel : IFromModel<TDBModel, TDTOModel>

{
    Task<TDTOModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TDTOModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<TDTOModel> CreateAsync(TDTOModel model, CancellationToken cancellationToken = default);
    Task<TDTOModel> UpdateAsync(TDTOModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class BaseService<TDBModel, TDTOModel, TStore> : IBaseService<TDBModel, TDTOModel>
    where TDBModel : IEntityDBModel
    where TDTOModel : IFromModel<TDBModel, TDTOModel>
    where TStore : IBaseStore<TDBModel>
{
    protected readonly TStore Store;

    public BaseService(TStore store) => Store = store;

    public virtual Task<TDTOModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Store.GetByIdAsync(id, cancellationToken).ContinueWith<TDTOModel?>(t => t.Result is null ? default : TDTOModel.FromModel(t.Result), cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTOModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => Store.GetAllAsync(cancellationToken).ContinueWith<IReadOnlyCollection<TDTOModel>>(t => t.Result.Select(TDTOModel.FromModel).ToList(), cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        CancellationToken cancellationToken = default)
        => Store.VectorAsync(predicate, cancellationToken).ContinueWith<IReadOnlyCollection<TDTOModel>>(t => t.Result.Select(TDTOModel.FromModel).ToList(), cancellationToken);

    public virtual Task<TDTOModel> CreateAsync(TDTOModel model, CancellationToken cancellationToken = default)
        => Store.CreateAsync(model.ToModel(), cancellationToken).ContinueWith<TDTOModel>(t => TDTOModel.FromModel(t.Result), cancellationToken);

    public virtual Task<TDTOModel> UpdateAsync(TDTOModel model, CancellationToken cancellationToken = default)
        => Store.UpdateAsync(model.ToModel(), cancellationToken).ContinueWith<TDTOModel>(t => TDTOModel.FromModel(t.Result), cancellationToken);

    public virtual Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Store.DeleteAsync(id, cancellationToken);
}
