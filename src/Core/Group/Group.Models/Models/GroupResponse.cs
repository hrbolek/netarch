using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Models;

public sealed record GroupResponse(
    Guid id,
    string Name,
    Guid group_type_id,
    Guid? master_group_id,
    string path,
    bool inherit_roles,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string? master_group_name = null,
    string? group_type_name = null,
    string visual_label = ""
) : IEntityDTOModel<GroupModel, GroupResponse>, ISecuredEntityDTOModel
{
    public static GroupResponse FromModel(GroupModel model) => new(
        model.id, model.Name, model.group_type_id, model.master_group_id, model.path, model.inherit_roles,
        model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by,
        model.master_group_name, model.group_type_name, model.Name);

    public static Expression<Func<GroupModel, GroupResponse>> Projection => model => new GroupResponse(
        model.id, model.Name, model.group_type_id, model.master_group_id, model.path, model.inherit_roles, model.rbacobject_id,
        model.created_at, model.updated_at, model.created_by, model.updated_by,
        model.master_group_name, model.group_type_name, model.Name);

    public GroupModel ToModel() => new(id, Name, group_type_id, master_group_id, path, inherit_roles, rbacobject_id,
        created_at, updated_at, created_by, updated_by)
    {
        master_group_name = master_group_name,
        group_type_name = group_type_name
    };
}
