using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Security.Models;

namespace WebAppArchitecture.Security.Services;

public interface ISecurityPermissionService
{
    Task<RbacPermissionResponse> GetPermissionsAsync(
        Guid rbacobjectId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only diagnostic projection of effective permissions for one RBAC object.
/// Runtime authorization remains in SecurityLayer.Query&lt;T&gt; and is not affected
/// by this UI/query service.
/// </summary>
public sealed class SecurityPermissionService(IDBLayer db) : ISecurityPermissionService
{
    public Task<RbacPermissionResponse> GetPermissionsAsync(
        Guid rbacobjectId,
        CancellationToken cancellationToken = default)
    {
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var accesses = db.Query<EffectiveAccessModel>()
            .Where(x => x.rbacobject_id == rbacobjectId
                && x.valid_from_unix_ms <= nowUnixMs
                && (x.valid_to_unix_ms == null || x.valid_to_unix_ms >= nowUnixMs))
            .ToArray();

        if (accesses.Length == 0)
        {
            return Task.FromResult(new RbacPermissionResponse(
                rbacobjectId,
                Array.Empty<RbacPermissionItemResponse>()));
        }

        var roleIds = accesses.Select(x => x.source_role_id).Distinct().ToArray();
        var roleTypeIds = accesses.Select(x => x.role_type_id).Distinct().ToArray();
        var groupIds = accesses.Select(x => x.source_group_id).Distinct().ToArray();
        var actorUserIds = accesses.Select(x => x.user_id).Distinct().ToArray();

        var roles = db.Query<RoleModel>()
            .Where(x => roleIds.Contains(x.id))
            .ToArray()
            .ToDictionary(x => x.id);

        var roleTypes = db.Query<RoleTypeModel>()
            .Where(x => roleTypeIds.Contains(x.id))
            .ToArray()
            .ToDictionary(x => x.id);

        var groups = db.Query<GroupModel>()
            .Where(x => groupIds.Contains(x.id))
            .ToArray()
            .ToDictionary(x => x.id);

        var users = db.Query<UserModel>()
            .Where(x => actorUserIds.Contains(x.id))
            .ToArray()
            .ToDictionary(x => x.id);

        var roleTypeOperationRows = db.Query<RoleTypeOperationModel>()
            .Where(x => roleTypeIds.Contains(x.role_type_id))
            .ToArray();

        var operationIds = roleTypeOperationRows
            .Select(x => x.operation_id)
            .Distinct()
            .ToArray();

        var operationsById = db.Query<OperationModel>()
            .Where(x => operationIds.Contains(x.id))
            .ToArray()
            .ToDictionary(x => x.id, x => x.name);

        var operationsByRoleType = roleTypeOperationRows
            .GroupBy(x => x.role_type_id)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<string>)g
                    .Select(x => operationsById.TryGetValue(x.operation_id, out var name) ? name : null)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray());

        var permissions = accesses
            .Select(access =>
            {
                roles.TryGetValue(access.source_role_id, out var role);
                roleTypes.TryGetValue(access.role_type_id, out var roleType);
                groups.TryGetValue(access.source_group_id, out var group);
                users.TryGetValue(access.user_id, out var user);
                operationsByRoleType.TryGetValue(access.role_type_id, out var operations);

                return new RbacPermissionItemResponse(
                    role_id: access.source_role_id,
                    group_id: access.source_group_id,
                    group_name: group?.Name,
                    user_id: access.user_id,
                    user_name: user?.Name,
                    user_email: user?.email,
                    role_type_id: access.role_type_id,
                    role_type: roleType?.name ?? access.role_type_id.ToString(),
                    is_deputy: role?.is_deputy ?? false,
                    valid_from: DateTimeOffset.FromUnixTimeMilliseconds(access.valid_from_unix_ms),
                    valid_to: access.valid_to_unix_ms is long validTo
                        ? DateTimeOffset.FromUnixTimeMilliseconds(validTo)
                        : null,
                    source_membership_id: access.source_membership_id == Guid.Empty
                        ? null
                        : access.source_membership_id,
                    source_access_binding_id: access.source_access_binding_id == Guid.Empty
                        ? null
                        : access.source_access_binding_id,
                    operations: operations ?? Array.Empty<string>());
            })
            .OrderBy(x => x.user_name ?? x.user_id.ToString())
            .ThenBy(x => x.role_type)
            .ToArray();

        return Task.FromResult(new RbacPermissionResponse(
            rbacobjectId,
            permissions));
    }
}
