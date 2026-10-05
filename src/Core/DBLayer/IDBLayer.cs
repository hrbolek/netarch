using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Central raw persistence gateway and scoped Unit of Work.
/// Domain Services must never inject this interface directly; application access goes through SecurityLayer.
/// Mutation methods stage changes in the current DbContext. SaveChanges and transaction boundaries are explicit.
/// </summary>
public interface IDBLayer
{
    IQueryable<TEntity> Query<TEntity>() where TEntity : class, IEntityDBModel;
    Task<TEntity?> GetByIdAsync<TEntity>(Guid id, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;

    Task<TEntity> StageCreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;
    Task<TEntity> StageUpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;
    Task StageDeleteAsync<TEntity>(Guid id, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;
    Task StageCreateRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;
    Task StageDeleteWhereAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;

    bool HasActiveTransaction { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default);
}
