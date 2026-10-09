using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.User.Models;

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
