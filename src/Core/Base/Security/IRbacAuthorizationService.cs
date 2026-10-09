using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Base.Security;

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

    public interface IRbacService : IRbacAuthorizationService
    {
        Task<bool> IsCurrentActorAllowedAsync(
        HttpContext httpContext,
        Guid rbacObjectId,
        string operation,
        CancellationToken cancellationToken = default);
    }
