using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Models;

public sealed record GroupTypeResponse(
    Guid id,
    string Name,
    Guid? parent_group_type_id,
    string path,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string? parent_group_type_name = null,
    string visual_label = ""
) : IEntityDTOModel<GroupTypeModel, GroupTypeResponse>, ISecuredEntityDTOModel
{
    public static GroupTypeResponse FromModel(GroupTypeModel model) => new(
        model.id, model.Name, model.parent_group_type_id, model.path, model.rbacobject_id,
        model.created_at, model.updated_at, model.created_by, model.updated_by, model.parent_group_type_name, model.Name);

    public static Expression<Func<GroupTypeModel, GroupTypeResponse>> Projection => model => new GroupTypeResponse(
        model.id, model.Name, model.parent_group_type_id, model.path, model.rbacobject_id,
        model.created_at, model.updated_at, model.created_by, model.updated_by, model.parent_group_type_name, model.Name);

    public GroupTypeModel ToModel() => new(id, Name, parent_group_type_id, path, rbacobject_id,
        created_at, updated_at, created_by, updated_by)
    {
        parent_group_type_name = parent_group_type_name
    };
}
