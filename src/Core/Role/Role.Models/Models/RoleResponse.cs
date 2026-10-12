using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Role.Models;

/// <summary>
/// Výstupní DTO entity Role response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record RoleResponse(
    Guid id,
    Guid group_id,
    Guid user_id,
    Guid role_type_id,
    string? role_type,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid? delegated_by_role_id,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string? group_name = null,
    string? user_name = null,
    string? delegated_by_role_type = null,
    string visual_label = ""
) : IEntityDTOModel<RoleModel, RoleResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static RoleResponse FromModel(RoleModel model) => new(
        model.id, model.group_id, model.user_id, model.role_type_id, model.role_type?.name,
        model.is_deputy, model.valid_from, model.valid_to, model.delegated_by_role_id,
        model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by,
        model.group_name, model.user_name, null,
        (model.role_type?.name ?? "Role") + " · " + (model.user_name ?? "User"));

    public static Expression<Func<RoleModel, RoleResponse>> Projection => model => new RoleResponse(
        model.id, model.group_id, model.user_id, model.role_type_id, model.role_type != null ? model.role_type.name : null,
        model.is_deputy, model.valid_from, model.valid_to, model.delegated_by_role_id,
        model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by,
        model.group_name, model.user_name, null,
        (model.role_type != null ? model.role_type.name : "Role") + " · " + (model.user_name ?? "User"));

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public RoleModel ToModel() => new(id, group_id, user_id, role_type_id, is_deputy, valid_from, valid_to,
        delegated_by_role_id, rbacobject_id, created_at, updated_at, created_by, updated_by)
    {
        group_name = group_name,
        user_name = user_name
    };
}
