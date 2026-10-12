using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Služba Security Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class SecurityService(IDBLayer db, ICurrentActorProvider actorProvider) : ISecurityService
{
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Query; pořadí a parametry viz implementace.
    /// </summary>
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

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: Query; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<bool> IsAllowedAsync(Guid actorUserId, Guid rbacObjectId, string operation, CancellationToken cancellationToken = default)
        => actorUserId != Guid.Empty && (await GetAllowedOperationsAsync(actorUserId, rbacObjectId, cancellationToken))
            .Contains(operation.Trim().ToLowerInvariant());

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: Query; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<bool> IsCurrentActorAllowedAsync(HttpContext httpContext, Guid rbacObjectId, string operation, CancellationToken cancellationToken = default)
    {
        var userId = actorProvider.TryGetActorUserId(httpContext);
        return userId is not null && await IsAllowedAsync(userId.Value, rbacObjectId, operation, cancellationToken);
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: Query; pořadí a parametry viz implementace.
    /// </summary>
    public Task<bool> CanAssignDeputyRoleAsync(Guid actorUserId, Guid groupId, string roleType, DateTimeOffset validFrom, DateTimeOffset? validTo, CancellationToken cancellationToken = default)
    {
        var roleTypeIds = db.Query<RoleTypeModel>().Where(x => x.name == roleType).Select(x => x.id);
        // Narrow candidates in SQL, then evaluate the validity predicate on the bounded result set.
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
