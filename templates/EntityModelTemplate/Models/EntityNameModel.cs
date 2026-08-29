using WebAppArchitecture.Base.Data;

namespace RootNamespace.EntityName.Models;

/// <summary>Persistence/data model.</summary>
public sealed record EntityNameModel(
    Guid id,
    string Name
) : IEntityDBModel;

/// <summary>Public resolver/UI DTO kept outside the resolver assembly.</summary>
public sealed record EntityNameResponse(
    Guid id,
    string Name
) : IEntityDTOModel<EntityNameModel>;

public interface IEntityNameStore : IBaseStore<EntityNameModel>
{
}

/// <summary>Demo store persistent for the lifetime of the host process.</summary>
public sealed class InMemoryEntityNameStore
    : InMemoryStore<EntityNameModel>, IEntityNameStore
{
}
