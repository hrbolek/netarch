using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Group.Models;

public sealed record GroupUpdateRequest(
    Guid id,
    string Name,
    Guid group_type_id,
    Guid? master_group_id = null,
    bool? inherit_roles = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<GroupModel>
{
    public static GroupUpdateRequest FromModel(GroupModel model) => new(
        model.id, model.Name, model.group_type_id, model.master_group_id, model.inherit_roles, model.updated_at, model.updated_by);

    public GroupModel ApplyTo(GroupModel model) => model with
    {
        Name = Name,
        group_type_id = group_type_id,
        master_group_id = master_group_id,
        inherit_roles = inherit_roles ?? model.inherit_roles,
        rbacobject_id = model.id,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    public GroupModel ToModel() => new(id, Name, group_type_id, master_group_id, string.Empty, inherit_roles ?? true, id,
        DateTimeOffset.UtcNow, updated_at ?? DateTimeOffset.UtcNow,
        Guid.Empty, updated_by ?? Guid.Empty);
}

/// <summary>Creates a child group. Authorization is evaluated on master_group_id.</summary>
