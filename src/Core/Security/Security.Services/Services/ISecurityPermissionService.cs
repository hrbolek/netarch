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
