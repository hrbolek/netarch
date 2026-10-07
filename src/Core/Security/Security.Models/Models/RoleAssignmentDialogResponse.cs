namespace WebAppArchitecture.Security.Models;

public sealed record RoleAssignmentDialogResponse(
    Guid rbacobject_id,
    string return_url,
    DateTimeOffset default_valid_from
);
