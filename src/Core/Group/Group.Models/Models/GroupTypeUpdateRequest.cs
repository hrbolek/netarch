using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Models;

public sealed record GroupTypeUpdateRequest(
    Guid id,
    string Name,
    Guid? parent_group_type_id = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<GroupTypeModel>
{
    public static GroupTypeUpdateRequest FromModel(GroupTypeModel model) => new(
        model.id, model.Name, model.parent_group_type_id, model.updated_at, model.updated_by);

    public GroupTypeModel ApplyTo(GroupTypeModel model) => model with
    {
        Name = Name,
        parent_group_type_id = parent_group_type_id,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    public GroupTypeModel ToModel() => new(id, Name, parent_group_type_id, string.Empty,
        RbacWellKnown.RootGroupId, DateTimeOffset.UtcNow, updated_at ?? DateTimeOffset.UtcNow,
        Guid.Empty, updated_by ?? Guid.Empty);
}
