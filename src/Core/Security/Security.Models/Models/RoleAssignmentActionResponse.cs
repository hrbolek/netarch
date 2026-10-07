namespace WebAppArchitecture.Security.Models;

public sealed record RoleAssignmentActionResponse(
    Guid rbacobject_id,
    string return_url,
    bool can_create_role
);
