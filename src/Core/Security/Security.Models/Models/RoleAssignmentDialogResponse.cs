namespace WebAppArchitecture.Security.Models;

public sealed record RoleAssignmentOption(Guid id, string label);

public sealed record RoleAssignmentDialogResponse(
    Guid rbacobject_id,
    string return_url,
    IReadOnlyCollection<RoleAssignmentOption> users,
    IReadOnlyCollection<RoleAssignmentOption> role_types,
    DateTimeOffset default_valid_from
);
