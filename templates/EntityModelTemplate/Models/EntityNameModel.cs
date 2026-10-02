using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.EntityName.Models;

/// <summary>Persistence/data model.</summary>
public sealed record EntityNameModel(
    Guid id,
    string Name
) : IEntityDBModel;

/// <summary>Public resolver/UI DTO kept outside the resolver assembly.</summary>
public sealed record EntityNameResponse(
    Guid id,
    string Name
) : IEntityDTOModel<EntityNameModel, EntityNameResponse>
{
    public static EntityNameResponse FromModel(EntityNameModel model)
        => new(
            model.id,
            model.Name
        );

    public EntityNameModel ToModel()
        => new(
            id,
            Name
        );        
};

public sealed record EntityNameCreateRequest(
    string Name
) : ICreateDTOModel<EntityNameModel>
{
    public EntityNameModel ToModel()
        => new(
            Guid.NewGuid(),
            Name
        );
};

public sealed record EntityNameUpdateRequest(
    Guid id,
    string Name
) : IUpdateDTOModel<EntityNameModel>, IEntityDTOModel<EntityNameModel, EntityNameUpdateRequest>
{
    public static EntityNameUpdateRequest FromModel(EntityNameModel model)
        => new(
            model.id,
            model.Name
        );

    public EntityNameModel ApplyTo(EntityNameModel model)
        => model with
        {
            Name = Name
        };
    public EntityNameModel ToModel()
        => new(
            id,
            Name
        );
};
public interface IEntityNameStore : IBaseStore<EntityNameModel>
{
}

/// <summary>Demo store persistent for the lifetime of the host process.</summary>
public sealed class InMemoryEntityNameStore
    : InMemoryStore<EntityNameModel>, IEntityNameStore
{
}
