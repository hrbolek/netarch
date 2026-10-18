using System.Text.Json.Serialization;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.GroupType.Models;

/// <summary>
/// Vstupní kontrakt Group Type Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record GroupTypeUpdateRequest(
    Guid id,
    [property: JsonRequired] DateTimeOffset updated_at,
    string Name,
    Guid? parent_group_type_id = null
) : IUpdateDTOModel<GroupTypeModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static GroupTypeUpdateRequest FromModel(GroupTypeModel model) => new(
        model.id, model.updated_at, model.Name, model.parent_group_type_id);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public GroupTypeModel ApplyTo(GroupTypeModel model) => model with
    {
        Name = Name,
        parent_group_type_id = parent_group_type_id
    };
}
