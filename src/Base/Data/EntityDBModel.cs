namespace WebAppArchitecture.Base.Data;

/// <summary>
/// Minimal contract shared by persistence and generic UI components.
/// The lowercase id intentionally follows the existing WebAppArchitecture models.
/// </summary>
public interface IEntityDBModel
{
    Guid id { get; }
}

public interface ISecuredEntityDBModel : IEntityDBModel
{
    Guid rbac_id { get; }
    DateTimeOffset created_at { get; }
    DateTimeOffset updated_at { get; }
    Guid created_by { get; }
    Guid updated_by { get; }
}

public interface IEntityDTOModel
{
    Guid id { get; }
}

public interface ISecuredEntityDTOModel : IEntityDTOModel
{
    Guid rbac_id { get; }
}

public interface ISecuredCreateRequest
{
    Guid? rbac_id { get; }
}

public interface IEntityDTOModel<TEntity, TSelf> : IEntityDTOModel, ICreateDTOModel<TEntity>
    where TEntity : IEntityDBModel
    where TSelf : IEntityDTOModel<TEntity, TSelf>
{
    static abstract TSelf FromModel(TEntity model);

}

public interface ICreateDTOModel<TEntity>
    where TEntity : IEntityDBModel
{
    TEntity ToModel();
}

public interface IUpdateDTOModel<TEntity>: IEntityDTOModel
    where TEntity : IEntityDBModel
{
    TEntity ApplyTo(TEntity model);
}