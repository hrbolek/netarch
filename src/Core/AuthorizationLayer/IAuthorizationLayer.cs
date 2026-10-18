using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.AuthorizationLayer;

/// <summary>
/// Authorization boundary for secured queries, controlled writes and transactions.
/// </summary>
public interface IAuthorizationLayer
{
    Guid CurrentUserId { get; }
    bool HasActiveTransaction { get; }

    IQueryable<TEntity> Query<TEntity>(string operation = SecurityOperations.Read)
        where TEntity : class, ISecuredEntityDBModel;

    Task<TEntity?> GetByIdAsync<TEntity>(Guid id, string operation = SecurityOperations.Read, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel;

    Task<TEntity> StageCreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel;

    Task<TEntity> StageUpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel;
    Task<TEntity> StageUpdateAsync<TEntity>(TEntity entity, DateTimeOffset expectedUpdatedAt, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel;

    Task StageDeleteAsync<TEntity>(Guid id, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default);

    Task<bool> IsAllowedAsync(Guid rbacId, string operation, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetAllowedOperationsAsync(
        IEnumerable<Guid> rbacIds,
        CancellationToken cancellationToken = default);
}
