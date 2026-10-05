using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

public interface ISecurityService : IRbacService
{
    Task<IReadOnlySet<string>> GetAllowedOperationsAsync(Guid userId, Guid rbacId, CancellationToken cancellationToken = default);
}

public sealed class SecurityService(IDBLayer db, ICurrentActorProvider actorProvider) : ISecurityService
{
    public Task<IReadOnlySet<string>> GetAllowedOperationsAsync(Guid userId, Guid rbacId, CancellationToken cancellationToken = default)
    {
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var roleTypeIds = db.Query<EffectiveAccessModel>()
            .Where(x => x.user_id == userId && x.rbacobject_id == rbacId && x.valid_from_unix_ms <= nowUnixMs && (x.valid_to_unix_ms == null || x.valid_to_unix_ms >= nowUnixMs))
            .Select(x => x.role_type_id);

        var operationIds = db.Query<RoleTypeOperationModel>()
            .Where(x => roleTypeIds.Contains(x.role_type_id))
            .Select(x => x.operation_id);

        var operations = db.Query<OperationModel>()
            .Where(x => operationIds.Contains(x.id))
            .Select(x => x.name)
            .ToArray()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Task.FromResult<IReadOnlySet<string>>(operations);
    }

    public async Task<bool> IsAllowedAsync(Guid actorUserId, Guid rbacObjectId, string operation, CancellationToken cancellationToken = default)
        => actorUserId != Guid.Empty && (await GetAllowedOperationsAsync(actorUserId, rbacObjectId, cancellationToken))
            .Contains(operation.Trim().ToLowerInvariant());

    public async Task<bool> IsCurrentActorAllowedAsync(HttpContext httpContext, Guid rbacObjectId, string operation, CancellationToken cancellationToken = default)
    {
        var userId = actorProvider.TryGetActorUserId(httpContext);
        return userId is not null && await IsAllowedAsync(userId.Value, rbacObjectId, operation, cancellationToken);
    }

    public Task<bool> CanAssignDeputyRoleAsync(Guid actorUserId, Guid groupId, string roleType, DateTimeOffset validFrom, DateTimeOffset? validTo, CancellationToken cancellationToken = default)
    {
        var roleTypeIds = db.Query<RoleTypeModel>().Where(x => x.name == roleType).Select(x => x.id);
        // SQLite cannot translate ordering comparisons over DateTimeOffset reliably.
        // First narrow the candidate set in SQL, then evaluate the small validity predicate in memory.
        var candidates = db.Query<RoleModel>()
            .Where(r => r.user_id == actorUserId
                && r.group_id == groupId
                && !r.is_deputy
                && roleTypeIds.Contains(r.role_type_id))
            .ToArray();

        var allowed = candidates.Any(r =>
            r.valid_from <= validFrom
            && (r.valid_to == null || (validTo != null && r.valid_to >= validTo)));

        return Task.FromResult(allowed);
    }
}
