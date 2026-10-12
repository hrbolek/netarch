using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Models;

/// <summary>
/// Vstupní kontrakt Group Type Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed record GroupTypeUpdateRequest(
    Guid id,
    string Name,
    Guid? parent_group_type_id = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<GroupTypeModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static GroupTypeUpdateRequest FromModel(GroupTypeModel model) => new(
        model.id, model.Name, model.parent_group_type_id, model.updated_at, model.updated_by);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public GroupTypeModel ApplyTo(GroupTypeModel model) => model with
    {
        Name = Name,
        parent_group_type_id = parent_group_type_id,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public GroupTypeModel ToModel() => new(id, Name, parent_group_type_id, string.Empty,
        RbacWellKnown.RootGroupId, DateTimeOffset.UtcNow, updated_at ?? DateTimeOffset.UtcNow,
        Guid.Empty, updated_by ?? Guid.Empty);
}
