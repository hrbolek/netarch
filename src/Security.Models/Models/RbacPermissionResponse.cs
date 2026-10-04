namespace WebAppArchitecture.Security.Models;

public sealed record RbacPermissionItemResponse(
    Guid role_id,
    Guid group_id,
    string? group_name,
    Guid user_id,
    string? user_name,
    string? user_email,
    Guid role_type_id,
    string role_type,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid? source_membership_id,
    IReadOnlyCollection<string> operations
);

public sealed record RbacPermissionResponse(
    Guid rbacobject_id,
    IReadOnlyCollection<RbacPermissionItemResponse> permissions
);
