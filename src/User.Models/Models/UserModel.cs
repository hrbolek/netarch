using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Models;

public sealed record UserResponse(
    Guid id,
    string Name,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDTOModel<UserModel, UserResponse>, ISecuredEntityDTOModel
{
    public static UserResponse FromModel(UserModel model)
        => new(model.id, model.Name, model.rbac_id, model.created_at, model.updated_at, model.created_by, model.updated_by);

    public static Expression<Func<UserModel, UserResponse>> Projection => model => new UserResponse(
        model.id, model.Name, model.rbac_id, model.created_at, model.updated_at, model.created_by, model.updated_by);

    public UserModel ToModel() => new(id, Name, rbac_id, created_at, updated_at, created_by, updated_by);
}

public sealed record UserCreateRequest(
    string Name,
    Guid? rbac_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<UserModel>, ISecuredCreateRequest
{
    public UserModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        return new(Guid.NewGuid(), Name, rbac_id ?? WebAppArchitecture.Base.Security.RbacWellKnown.RootSecurityObjectId,
            created_at ?? now, updated_at ?? now, created_by ?? Guid.Empty, updated_by ?? Guid.Empty);
    }
}

public sealed record UserUpdateRequest(
    Guid id,
    string Name,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<UserModel>
{
    public static UserUpdateRequest FromModel(UserModel model)
        => new(model.id, model.Name, model.updated_at, model.updated_by);

    public UserModel ApplyTo(UserModel model) => model with
    {
        Name = Name,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    public UserModel ToModel() => new(id, Name, WebAppArchitecture.Base.Security.RbacWellKnown.RootSecurityObjectId,
        DateTimeOffset.UtcNow, updated_at ?? DateTimeOffset.UtcNow, Guid.Empty, updated_by ?? Guid.Empty);
}
