namespace WebAppArchitecture.Security.Models;

/// <summary>
/// Výstupní DTO entity Role Assignment Action response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record RoleAssignmentActionResponse(
    Guid rbacobject_id,
    string return_url,
    bool can_create_role
);
