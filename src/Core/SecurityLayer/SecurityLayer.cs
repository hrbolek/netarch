using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

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
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var operationId = RbacOperationIds.FromName(operation);

        // Keep the RBAC lookup in SQL, using the same role-to-operation
        // mapping as SecurityService.GetAllowedOperationsAsync. A subquery
        // over permitted object IDs also composes with caller Where/Count/etc.
        var accessibleObjects =
            from access in db.Query<EffectiveAccessModel>()
            join mapping in db.Query<RoleTypeOperationModel>()
                on access.role_type_id equals mapping.role_type_id
            where access.user_id == userId
                && access.valid_from_unix_ms <= nowUnixMs
                && (access.valid_to_unix_ms == null || access.valid_to_unix_ms >= nowUnixMs)
                && mapping.operation_id == operationId
            select access.rbacobject_id;

        return db.Query<TEntity>()
            .Where(entity => accessibleObjects.Contains(entity.rbacobject_id));
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
        if (!await IsAllowedAsync(entity.rbacobject_id, SecurityOperations.Update, cancellationToken))
            throw new UnauthorizedAccessException("Update operation is not allowed for this RBAC context.");
        return await db.StageUpdateAsync(entity, cancellationToken);
    }

    public async Task StageDeleteAsync<TEntity>(Guid id, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel
    {
        var current = await db.GetByIdAsync<TEntity>(id, cancellationToken);
        if (current is null) return;
        if (!await IsAllowedAsync(current.rbacobject_id, SecurityOperations.Delete, cancellationToken))
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
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var rows = (
            from access in db.Query<EffectiveAccessModel>()
            join mapping in db.Query<RoleTypeOperationModel>()
                on access.role_type_id equals mapping.role_type_id
            join operation in db.Query<OperationModel>()
                on mapping.operation_id equals operation.id
            where access.user_id == userId
                && ids.Contains(access.rbacobject_id)
                && access.valid_from_unix_ms <= nowUnixMs
                && (access.valid_to_unix_ms == null || access.valid_to_unix_ms >= nowUnixMs)
            select new { access.rbacobject_id, operation.name }
        ).Distinct().ToArray();

        IReadOnlyDictionary<Guid, IReadOnlySet<string>> result = ids.ToDictionary(
            id => id,
            id => (IReadOnlySet<string>)rows
                .Where(x => x.rbacobject_id == id)
                .Select(x => x.name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));

        return Task.FromResult(result);
    }
}
