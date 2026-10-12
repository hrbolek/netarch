namespace WebAppArchitecture.Security.Models;

/// <summary>
/// Výstupní DTO entity Rbac Permission Item response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
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
    Guid? source_access_binding_id,
    IReadOnlyCollection<string> operations
);
