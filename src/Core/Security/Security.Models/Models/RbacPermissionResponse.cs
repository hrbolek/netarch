namespace WebAppArchitecture.Security.Models;

public sealed record RbacPermissionResponse(
    Guid rbacobject_id,
    IReadOnlyCollection<RbacPermissionItemResponse> permissions
);
