using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Models;

public sealed record UserResponse(
    Guid id,
    string Name,
    string? email,
    string? surname,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<UserModel, UserResponse>, ISecuredEntityDTOModel
{
    public static UserResponse FromModel(UserModel model)
        => new(model.id, model.Name, model.email, model.surname, model.rbacobject_id,
            model.created_at, model.updated_at, model.created_by, model.updated_by,
            string.IsNullOrWhiteSpace($"{model.Name} {model.surname}".Trim())
                ? (model.email ?? "User")
                : $"{model.Name} {model.surname}".Trim());

    public static Expression<Func<UserModel, UserResponse>> Projection => model => new UserResponse(
        model.id, model.Name, model.email, model.surname, model.rbacobject_id,
        model.created_at, model.updated_at, model.created_by, model.updated_by,
        (model.Name + " " + (model.surname ?? "")).Trim() != ""
            ? (model.Name + " " + (model.surname ?? "")).Trim()
            : (model.email ?? "User"));

    public UserModel ToModel() => new(
        id, Name, email, surname, rbacobject_id, created_at, updated_at, created_by, updated_by);
}


public sealed record UserUpdateRequest(
    Guid id,
    string Name,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null,
    string? email = null,
    string? surname = null
) : IUpdateDTOModel<UserModel>
{
    public static UserUpdateRequest FromModel(UserModel model)
        => new(model.id, model.Name, model.updated_at, model.updated_by, model.email, model.surname);

    public UserModel ApplyTo(UserModel model) => model with
    {
        Name = Name,
        email = email ?? model.email,
        surname = surname ?? model.surname,
        updated_at = updated_at ?? DateTimeOffset.UtcNow,
        updated_by = updated_by ?? model.updated_by
    };

    public UserModel ToModel() => new(
        id,
        Name,
        email,
        surname,
        WebAppArchitecture.Base.Security.RbacWellKnown.RootSecurityObjectId,
        DateTimeOffset.UtcNow,
        updated_at ?? DateTimeOffset.UtcNow,
        Guid.Empty,
        updated_by ?? Guid.Empty);
}

/// <summary>Safe public create contract: a user is created and immediately contained in an existing Group.</summary>
public sealed record UserCreateContainedRequest(
    string Name,
    Guid group_id,
    string? email = null,
    string? surname = null,
    DateTimeOffset? membership_valid_from = null,
    DateTimeOffset? membership_valid_to = null);
