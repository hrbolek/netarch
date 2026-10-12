using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Models;

/// <summary>
/// Vstupní kontrakt User Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record UserUpdateRequest(
    Guid id,
    string Name,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null,
    string? email = null,
    string? surname = null
) : IUpdateDTOModel<UserModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static UserUpdateRequest FromModel(UserModel model)
        => new(model.id, model.Name, model.updated_at, model.updated_by, model.email, model.surname);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: RbacWellKnown; pořadí a parametry viz implementace.
    /// </summary>
    public UserModel ApplyTo(UserModel model) => model with
    {
        Name = Name,
        email = email ?? model.email,
        surname = surname ?? model.surname,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: RbacWellKnown; pořadí a parametry viz implementace.
    /// </summary>
    public UserModel ToModel() => new(
        id,
        Name,
        email,
        surname,
        WebAppArchitecture.Base.Security.RbacWellKnown.RootSecurityObjectId,
        DateTimeOffset.UtcNow,
        updated_at ?? DateTimeOffset.UtcNow,
        Guid.Empty,
        updated_by ?? Guid.Empty);
}

/// <summary>Safe public create contract: a user is created and immediately contained in an existing Group.</summary>
