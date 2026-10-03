using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

public interface ISecurityProjectionService
{
    Task RebuildAllAsync(CancellationToken cancellationToken = default);
}

public interface ISecurityLayer
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

public sealed class SecurityLayer(
    IDBLayer db,
    ISecurityService securityService,
    ICurrentActorProvider actorProvider,
    IHttpContextAccessor httpContextAccessor) : ISecurityLayer
{
    public bool HasActiveTransaction => db.HasActiveTransaction;

    public Guid CurrentUserId
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext
                ?? throw new UnauthorizedAccessException("No current HTTP context is available.");
            return actorProvider.TryGetActorUserId(httpContext)
                ?? throw new UnauthorizedAccessException("Current user identity is missing.");
        }
    }

    public IQueryable<TEntity> Query<TEntity>(string operation = SecurityOperations.Read)
        where TEntity : class, ISecuredEntityDBModel
    {
        var userId = CurrentUserId;
        var now = DateTimeOffset.UtcNow;
        var operationName = operation.Trim().ToLowerInvariant();

        var effective = db.Query<EffectiveAccessModel>();
        var mappings = db.Query<RoleTypeOperationModel>();
        var operations = db.Query<OperationModel>();

        return db.Query<TEntity>().Where(entity =>
            effective.Any(access =>
                access.user_id == userId &&
                access.rbac_id == entity.rbac_id &&
                access.valid_from <= now &&
                (access.valid_to == null || access.valid_to >= now) &&
                mappings.Any(map => map.role_type_id == access.role_type_id &&
                    operations.Any(op => op.id == map.operation_id && op.name == operationName))));
    }

    public Task<TEntity?> GetByIdAsync<TEntity>(Guid id, string operation = SecurityOperations.Read, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel
        => Task.FromResult(Query<TEntity>(operation).SingleOrDefault(x => x.id == id));

    public Task<TEntity> StageCreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel
        => db.StageCreateAsync(entity, cancellationToken);

    public async Task<TEntity> StageUpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel
    {
        if (!await IsAllowedAsync(entity.rbac_id, SecurityOperations.Update, cancellationToken))
            throw new UnauthorizedAccessException("Update operation is not allowed for this RBAC context.");
        return await db.StageUpdateAsync(entity, cancellationToken);
    }

    public async Task StageDeleteAsync<TEntity>(Guid id, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel
    {
        var current = await db.GetByIdAsync<TEntity>(id, cancellationToken);
        if (current is null) return;
        if (!await IsAllowedAsync(current.rbac_id, SecurityOperations.Delete, cancellationToken))
            throw new UnauthorizedAccessException("Delete operation is not allowed for this RBAC context.");
        await db.StageDeleteAsync<TEntity>(id, cancellationToken);
    }


    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(action, cancellationToken);

    public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(action, cancellationToken);

    public Task<bool> IsAllowedAsync(Guid rbacId, string operation, CancellationToken cancellationToken = default)
        => securityService.IsAllowedAsync(CurrentUserId, rbacId, operation, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, IReadOnlySet<string>>> GetAllowedOperationsAsync(
        IEnumerable<Guid> rbacIds,
        CancellationToken cancellationToken = default)
    {
        var ids = rbacIds.Distinct().ToArray();
        if (ids.Length == 0)
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlySet<string>>>(
                new Dictionary<Guid, IReadOnlySet<string>>());

        var userId = CurrentUserId;
        var now = DateTimeOffset.UtcNow;

        var rows = (
            from access in db.Query<EffectiveAccessModel>()
            join mapping in db.Query<RoleTypeOperationModel>()
                on access.role_type_id equals mapping.role_type_id
            join operation in db.Query<OperationModel>()
                on mapping.operation_id equals operation.id
            where access.user_id == userId
                && ids.Contains(access.rbac_id)
                && access.valid_from <= now
                && (access.valid_to == null || access.valid_to >= now)
            select new { access.rbac_id, operation.name }
        ).Distinct().ToArray();

        IReadOnlyDictionary<Guid, IReadOnlySet<string>> result = ids.ToDictionary(
            id => id,
            id => (IReadOnlySet<string>)rows
                .Where(x => x.rbac_id == id)
                .Select(x => x.name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));

        return Task.FromResult(result);
    }
}


