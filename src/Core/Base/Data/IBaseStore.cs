using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace WebAppArchitecture.Base.Data;

public interface IBaseStore<TModel>
    where TModel : IEntityDBModel
{
    IQueryable<TModel> Query() => throw new NotSupportedException("Queryable access is not implemented by this store.");
    Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TModel>> VectorAsync(
        Expression<Func<TModel, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<TModel> CreateAsync(TModel model, CancellationToken cancellationToken = default);
    Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Demo store persistent only for the lifetime of the process.
/// </summary>
