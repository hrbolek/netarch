using System.Text.Json.Serialization;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Models;

/// <summary>
/// Vstupní kontrakt User Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record UserUpdateRequest(
    Guid id,
    [property: JsonRequired] DateTimeOffset updated_at,
    string Name,
    string? email = null,
    string? surname = null
) : IUpdateDTOModel<UserModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static UserUpdateRequest FromModel(UserModel model)
        => new(model.id, model.updated_at, model.Name, model.email, model.surname);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: RbacWellKnown; pořadí a parametry viz implementace.
    /// </summary>
    public UserModel ApplyTo(UserModel model) => model with
    {
        Name = Name,
        email = email ?? model.email,
        surname = surname ?? model.surname
    };
}

