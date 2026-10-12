namespace WebAppArchitecture.Security.Models;

/// <summary>
/// Výstupní DTO entity Role Assignment Dialog response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record RoleAssignmentDialogResponse(
    Guid rbacobject_id,
    string return_url,
    DateTimeOffset default_valid_from
);
