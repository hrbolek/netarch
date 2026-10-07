using System.Linq.Expressions;

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
    Guid rbacobject_id { get; }
    DateTimeOffset created_at { get; }
    DateTimeOffset updated_at { get; }
    Guid created_by { get; }
    Guid updated_by { get; }
}

public interface IEntityDTOModel
{
    Guid id { get; }
    string visual_label => id.ToString();
}

public interface ISecuredEntityDTOModel : IEntityDTOModel
{
    Guid rbacobject_id { get; }
}

public interface IEntityDTOModel<TEntity, TSelf> : IEntityDTOModel, ICreateDTOModel<TEntity>
    where TEntity : IEntityDBModel
    where TSelf : IEntityDTOModel<TEntity, TSelf>
{
    static abstract TSelf FromModel(TEntity model);
    static abstract Expression<Func<TEntity, TSelf>> Projection { get; }

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
