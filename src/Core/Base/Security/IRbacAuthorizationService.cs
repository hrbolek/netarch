using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Base.Security;

/// <summary>Authorization decisions, not domain entity access.</summary>
/// <remarks>
/// Architecture exception: resolvers may use this interface without a SecuredBaseService
/// implementation because authorization is a prerequisite of that base, not an entity query.
/// Implementations must not depend recursively on the secured entity pipeline. Keep domain
/// reads and mutations in their secured services; a permission decision is not a data gateway.
/// </remarks>
public interface IRbacAuthorizationService
{
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

    /// <summary>
    /// Current-actor adapter for the authorization-substrate exception documented on
    /// IRbacAuthorizationService; it does not permit direct domain data access from resolvers.
    /// </summary>
    public interface IRbacService : IRbacAuthorizationService
    {
        Task<bool> IsCurrentActorAllowedAsync(
        HttpContext httpContext,
        Guid rbacObjectId,
        string operation,
        CancellationToken cancellationToken = default);
    }
