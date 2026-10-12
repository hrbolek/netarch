using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Role.Models;

/// <summary>
/// Vstupní kontrakt Role Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record RoleUpdateRequest(
    Guid id,
    Guid role_type_id,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<RoleModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static RoleUpdateRequest FromModel(RoleModel model)
        => new(model.id, model.role_type_id, model.is_deputy, model.valid_from, model.valid_to, model.updated_at, model.updated_by);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public RoleModel ApplyTo(RoleModel model) => model with
    {
        role_type_id = role_type_id,
        is_deputy = is_deputy,
        valid_from = valid_from,
        valid_to = valid_to,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public RoleModel ToModel() => new(id, Guid.Empty, Guid.Empty, role_type_id, is_deputy, valid_from, valid_to,
        null, RbacWellKnown.RootSecurityObjectId, DateTimeOffset.UtcNow,
        updated_at ?? DateTimeOffset.UtcNow, Guid.Empty, updated_by ?? Guid.Empty);
}

/// <summary>Safe public create contract. Authorization is evaluated on group_id.</summary>
