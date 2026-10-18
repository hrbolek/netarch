namespace WebAppArchitecture.Security.Models;

/// <summary>
/// Výstupní DTO entity Rbac Permission response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>

public sealed record RbacPermissionResponse(
    Guid rbacobject_id,
    IReadOnlyCollection<RbacPermissionItemResponse> permissions
);
