using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.User.Models;

/// <summary>Persistence/data model.</summary>
public sealed record UserModel(
    Guid id,
    string Name
) : IEntityDBModel;

/// <summary>Public resolver/UI DTO kept outside the resolver assembly.</summary>
public sealed record UserResponse(
    Guid id,
    string Name
) : IEntityDTOModel<UserModel, UserResponse>
{
    public static UserResponse FromModel(UserModel model)
        => new(
            model.id,
            model.Name
        );

    public UserModel ToModel()
        => new(
            id,
            Name
        );        
};

public sealed record UserCreateRequest(
    string Name
) : ICreateDTOModel<UserModel>
{
    public UserModel ToModel()
        => new(
            Guid.NewGuid(),
            Name
        );
};

public sealed record UserUpdateRequest(
    Guid id,
    string Name
) : IUpdateDTOModel<UserModel>, IEntityDTOModel<UserModel, UserUpdateRequest>
{
    public static UserUpdateRequest FromModel(UserModel model)
        => new(
            model.id,
            model.Name
        );

    public UserModel ApplyTo(UserModel model)
        => model with
        {
            Name = Name
        };
    public UserModel ToModel()
        => new(
            id,
            Name
        );
};
public interface IUserStore : IBaseStore<UserModel>
{
}

/// <summary>Demo store persistent for the lifetime of the host process.</summary>
public sealed class InMemoryUserStore
    : InMemoryStore<UserModel>, IUserStore
{
}
