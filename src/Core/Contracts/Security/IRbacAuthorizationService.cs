namespace WebAppArchitecture.Base.Security;

/// <summary>Authorization decisions, not domain entity access.</summary>
/// <remarks>
/// Architecture exception: resolvers may use this interface without a SecuredEntityService
/// implementation because authorization is a prerequisite of that base, not an entity query.
/// Implementations must not depend recursively on the secured entity pipeline. Keep domain
/// reads and mutations in their secured services; a permission decision is not a data gateway.
/// </remarks>

public interface IRbacAuthorizationService
{
    Task<IReadOnlySet<string>> GetAllowedOperationsAsync(
        Guid userId,
        Guid rbacId,
        CancellationToken cancellationToken = default);

    Task<bool> IsAllowedAsync(
        Guid actorUserId,
        Guid rbacObjectId,
        string operation,
        CancellationToken cancellationToken = default);

    Task<bool> CanAssignDeputyRoleAsync(
        Guid actorUserId,
        Guid groupId,
        string roleType,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken = default);
}
