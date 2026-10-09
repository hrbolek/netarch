using System.Linq.Expressions;

namespace WebAppArchitecture.Base.Data;

/// <summary>
/// DTO contract for converting and projecting a database entity to its response model.
/// </summary>
public interface IEntityDTOModel<TEntity, TSelf> : IEntityDTOModel, ICreateDTOModel<TEntity>
    where TEntity : IEntityDBModel
    where TSelf : IEntityDTOModel<TEntity, TSelf>
{
    static abstract TSelf FromModel(TEntity model);
    static abstract Expression<Func<TEntity, TSelf>> Projection { get; }
}
