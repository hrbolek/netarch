using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
        var now = DateTimeOffset.UtcNow;
        var roleTypeIds = db.Query<EffectiveAccessModel>()
            .Where(x => x.user_id == userId && x.rbac_id == rbacId && x.valid_from <= now && (x.valid_to == null || x.valid_to >= now))
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
        var allowed = db.Query<RoleModel>().Any(r =>
            r.user_id == actorUserId && r.group_id == groupId && !r.is_deputy && roleTypeIds.Contains(r.role_type_id) &&
            r.valid_from <= validFrom && (r.valid_to == null || (validTo != null && r.valid_to >= validTo)));
        return Task.FromResult(allowed);
    }
}

public static class SecurityEndpointExtensions
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup(prefix.TrimEnd('/') + "/security").WithTags("Security");
        group.MapGet("/{rbacId:guid}/operations", async (Guid rbacId, Guid? userId, HttpContext httpContext, ISecurityService service, ICurrentActorProvider actorProvider, CancellationToken cancellationToken) =>
        {
            var effectiveUserId = userId ?? actorProvider.TryGetActorUserId(httpContext);
            if (effectiveUserId is null) return Results.Unauthorized();
            var operations = await service.GetAllowedOperationsAsync(effectiveUserId.Value, rbacId, cancellationToken);
            return Results.Ok(operations.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
        });
        return endpoints;
    }
}
