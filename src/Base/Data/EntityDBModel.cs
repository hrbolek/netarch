namespace WebAppArchitecture.Base.Data;

/// <summary>
/// Minimal contract shared by persistence and generic UI components.
/// The lowercase id intentionally follows the existing WebAppArchitecture models.
/// </summary>
public interface IEntityDBModel
{
    Guid id { get; }
}

public interface IEntityDTOModel<TEntity>
    where TEntity : IEntityDBModel
{
    TEntity ToEntity();
}

public interface IFromModel<IEntityDBModel, IEntityDTOModel>
    where IEntityDTOModel : IFromModel<IEntityDBModel, IEntityDTOModel>
{
    static abstract IEntityDTOModel FromModel(
        IEntityDBModel model);

    abstract IEntityDBModel ToModel();
}