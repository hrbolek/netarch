using System.ComponentModel.DataAnnotations.Schema;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Identita uživatele, ke které se přiřazují role, členství a auditní údaje; oprávnění se hodnotí vůči efektivní identitě.
/// </summary>

public sealed record UserModel(
    Guid id,
    string Name,
    string? email,
    string? surname,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;
