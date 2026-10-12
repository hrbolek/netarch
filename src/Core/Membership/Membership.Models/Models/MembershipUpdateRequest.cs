using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Membership.Models;

/// <summary>
/// Vstupní kontrakt Membership Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record MembershipUpdateRequest(
    Guid id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<MembershipModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static MembershipUpdateRequest FromModel(MembershipModel model)
        => new(model.id, model.valid_from, model.valid_to, model.updated_at, model.updated_by);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public MembershipModel ApplyTo(MembershipModel model) => model with
    {
        valid_from = valid_from,
        valid_to = valid_to,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public MembershipModel ToModel() => new(id, Guid.Empty, Guid.Empty, valid_from, valid_to,
        RbacWellKnown.RootSecurityObjectId, DateTimeOffset.UtcNow,
        updated_at ?? DateTimeOffset.UtcNow, Guid.Empty, updated_by ?? Guid.Empty);
}

/// <summary>Safe public create contract. Authorization is evaluated on group_id.</summary>
