using System.Text.Json.Serialization;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Models;

/// <summary>
/// Vstupní kontrakt Group Update Request. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>

public sealed record GroupUpdateRequest(
    Guid id,
    [property: JsonRequired] DateTimeOffset updated_at,
    string Name,
    Guid group_type_id,
    Guid? master_group_id = null,
    bool? inherit_roles = null
) : IUpdateDTOModel<GroupModel>
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static GroupUpdateRequest FromModel(GroupModel model) => new(
        model.id, model.updated_at, model.Name, model.group_type_id, model.master_group_id, model.inherit_roles);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public GroupModel ApplyTo(GroupModel model) => model with
    {
        Name = Name,
        group_type_id = group_type_id,
        master_group_id = master_group_id,
        inherit_roles = inherit_roles ?? model.inherit_roles,
        rbacobject_id = model.id
    };
}

