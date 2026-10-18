using System.Text.Json.Serialization;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Models;

/// <summary>
/// Vstupní kontrakt Membership Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record MembershipUpdateRequest(
    Guid id,
    [property: JsonRequired] DateTimeOffset updated_at,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null
) : IUpdateDTOModel<MembershipModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static MembershipUpdateRequest FromModel(MembershipModel model)
        => new(model.id, model.updated_at, model.valid_from, model.valid_to);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public MembershipModel ApplyTo(MembershipModel model) => model with
    {
        valid_from = valid_from,
        valid_to = valid_to
    };
}

