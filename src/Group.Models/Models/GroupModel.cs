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
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string? parent_group_type_name = null
) : IEntityDTOModel<GroupTypeModel, GroupTypeResponse>, ISecuredEntityDTOModel
{
    public static GroupTypeResponse FromModel(GroupTypeModel model) => new(
        model.id, model.Name, model.parent_group_type_id, model.path, model.rbac_id,
        model.created_at, model.updated_at, model.created_by, model.updated_by, model.parent_group_type_name);

    public static Expression<Func<GroupTypeModel, GroupTypeResponse>> Projection => model => new GroupTypeResponse(
        model.id, model.Name, model.parent_group_type_id, model.path, model.rbac_id,
        model.created_at, model.updated_at, model.created_by, model.updated_by, model.parent_group_type_name);

    public GroupTypeModel ToModel() => new(id, Name, parent_group_type_id, path, rbac_id,
        created_at, updated_at, created_by, updated_by, parent_group_type_name);
}

public sealed record GroupTypeCreateRequest(
    string Name,
    Guid? parent_group_type_id = null,
    Guid? rbac_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<GroupTypeModel>, ISecuredCreateRequest
{
    public GroupTypeModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        return new(id, Name, parent_group_type_id, string.Empty,
            rbac_id ?? RbacWellKnown.RootGroupId,
            created_at ?? now, updated_at ?? now, created_by ?? Guid.Empty, updated_by ?? Guid.Empty);
    }
}

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

public sealed record GroupResponse(
    Guid id,
    string Name,
    Guid group_type_id,
    Guid? master_group_id,
    string path,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string? master_group_name = null,
    string? group_type_name = null
) : IEntityDTOModel<GroupModel, GroupResponse>, ISecuredEntityDTOModel
{
    public static GroupResponse FromModel(GroupModel model) => new(
        model.id, model.Name, model.group_type_id, model.master_group_id, model.path,
        model.rbac_id, model.created_at, model.updated_at, model.created_by, model.updated_by,
        model.master_group_name, model.group_type_name);

    public static Expression<Func<GroupModel, GroupResponse>> Projection => model => new GroupResponse(
        model.id, model.Name, model.group_type_id, model.master_group_id, model.path, model.rbac_id,
        model.created_at, model.updated_at, model.created_by, model.updated_by,
        model.master_group_name, model.group_type_name);

    public GroupModel ToModel() => new(id, Name, group_type_id, master_group_id, path, rbac_id,
        created_at, updated_at, created_by, updated_by, master_group_name, group_type_name);
}

public sealed record GroupCreateRequest(
    string Name,
    Guid? group_type_id = null,
    Guid? master_group_id = null,
    Guid? rbac_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<GroupModel>, ISecuredCreateRequest
{
    public GroupModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        return new(id, Name, group_type_id ?? RbacWellKnown.RootGroupTypeId, master_group_id,
            string.Empty, id, created_at ?? now, updated_at ?? now,
            created_by ?? Guid.Empty, updated_by ?? Guid.Empty);
    }
}

public sealed record GroupUpdateRequest(
    Guid id,
    string Name,
    Guid group_type_id,
    Guid? master_group_id = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<GroupModel>
{
    public static GroupUpdateRequest FromModel(GroupModel model) => new(
        model.id, model.Name, model.group_type_id, model.master_group_id, model.updated_at, model.updated_by);

    public GroupModel ApplyTo(GroupModel model) => model with
    {
        Name = Name,
        group_type_id = group_type_id,
        master_group_id = master_group_id,
        rbac_id = model.id,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    public GroupModel ToModel() => new(id, Name, group_type_id, master_group_id, string.Empty, id,
        DateTimeOffset.UtcNow, updated_at ?? DateTimeOffset.UtcNow,
        Guid.Empty, updated_by ?? Guid.Empty);
}
