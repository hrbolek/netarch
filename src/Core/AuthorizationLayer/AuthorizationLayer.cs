using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.AuthorizationLayer;

/// <summary>
/// Enforces RBAC for secured entity access through the database layer.
/// </summary>
public sealed class AuthorizationLayer(
    IDBLayer db,
    IRbacAuthorizationService securityService,
    ICurrentActorProvider actorProvider,
    IHttpContextAccessor httpContextAccessor) : IAuthorizationLayer
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
        // mapping as RbacAuthorizationService.GetAllowedOperationsAsync. A subquery
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

    /// <summary>
    /// Creation is authorized on the target security scope, not on the new row id.
    /// New RBAC groups are authorized against their existing parent scope.
    /// </summary>
    public async Task<TEntity> StageCreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel
    {
        var scope = entity is GroupModel group && group.master_group_id.HasValue
            ? group.master_group_id.Value : entity.rbacobject_id;
        if (scope == Guid.Empty || !await IsAllowedAsync(scope, SecurityOperations.Create, cancellationToken))
            throw new UnauthorizedAccessException("Create operation is not allowed for the target RBAC context.");

        if (entity is RoleModel role && role.is_deputy)
        {
            // A deputy must be delegated by an active, direct role held by the current actor.
            // A deputy cannot create additional deputies and cannot extend the source period.
            if (!role.delegated_by_role_id.HasValue)
                throw new UnauthorizedAccessException("Deputy role requires a source role.");
            var source = await db.GetByIdAsync<RoleModel>(role.delegated_by_role_id.Value, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            if (source is null || source.user_id != CurrentUserId || source.is_deputy ||
                source.valid_from > now || source.valid_to is not null && source.valid_to < now ||
                source.group_id != role.group_id || source.role_type_id != role.role_type_id ||
                source.valid_from > role.valid_from || role.valid_to is null && source.valid_to is not null ||
                source.valid_to is not null && role.valid_to > source.valid_to ||
                role.valid_to is not null && role.valid_to < role.valid_from)
                throw new UnauthorizedAccessException("Invalid deputy delegation scope or validity.");
        }
        return await db.StageAuditedCreateAsync(entity, CurrentUserId, cancellationToken);
    }

    public async Task<TEntity> StageUpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel
    {
        var current = await db.GetByIdAsync<TEntity>(entity.id, cancellationToken)
            ?? throw new InvalidOperationException("Cannot update a missing secured entity.");
        return await StageUpdateAsync(entity, current.updated_at, cancellationToken);
    }

    public async Task<TEntity> StageUpdateAsync<TEntity>(TEntity entity, DateTimeOffset expectedUpdatedAt, CancellationToken cancellationToken = default)
        where TEntity : class, ISecuredEntityDBModel
    {
        var current = await db.GetByIdAsync<TEntity>(entity.id, cancellationToken)
            ?? throw new InvalidOperationException("Cannot update a missing secured entity.");
        if (!await IsAllowedAsync(current.rbacobject_id, SecurityOperations.Update, cancellationToken))
            throw new UnauthorizedAccessException("Update operation is not allowed for the original RBAC context.");
        if (current.rbacobject_id != entity.rbacobject_id &&
            !await IsAllowedAsync(entity.rbacobject_id, SecurityOperations.Create, cancellationToken))
            throw new UnauthorizedAccessException("Moving a secured row requires Create on the target RBAC context.");
        if (current is RoleModel oldRole && entity is RoleModel newRole &&
            (oldRole.user_id != newRole.user_id || oldRole.role_type_id != newRole.role_type_id ||
             oldRole.group_id != newRole.group_id || oldRole.is_deputy != newRole.is_deputy ||
             oldRole.delegated_by_role_id != newRole.delegated_by_role_id))
            throw new UnauthorizedAccessException("Role identity and delegation cannot be modified through generic Update; revoke and issue a new role.");
        return await db.StageAuditedUpdateAsync(entity, current, expectedUpdatedAt, CurrentUserId, cancellationToken);
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


    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: SaveChangesAsync, ExecuteInTransactionAsync; pořadí a parametry viz implementace.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: ExecuteInTransactionAsync; pořadí a parametry viz implementace.
    /// </summary>
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(action, cancellationToken);

    public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(action, cancellationToken);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
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
