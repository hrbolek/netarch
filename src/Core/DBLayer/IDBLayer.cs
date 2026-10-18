using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Central raw persistence gateway and scoped Unit of Work.
/// Domain Services must never inject this interface directly; application access goes through AuthorizationLayer.
/// Mutation methods stage changes in the current DbContext. SaveChanges and transaction boundaries are explicit.
/// </summary>
/// <summary>
/// Typ I D B Layer ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public interface IDBLayer
{
    IQueryable<TEntity> Query<TEntity>() where TEntity : class, IEntityDBModel;
    Task<TEntity?> GetByIdAsync<TEntity>(Guid id, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;

    Task<TEntity> StageCreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;
    Task<TEntity> StageUpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;
    Task<TEntity> StageAuditedCreateAsync<TEntity>(TEntity entity, Guid actor, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel;
    Task<TEntity> StageAuditedUpdateAsync<TEntity>(TEntity entity, TEntity original, DateTimeOffset expectedUpdatedAt, Guid actor, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel;
    Task StageDeleteAsync<TEntity>(Guid id, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;
    Task StageCreateRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;
    Task StageDeleteWhereAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;
    Task<int> DeleteWhereAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel;

    bool HasActiveTransaction { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default);
}
