using System.Linq.Expressions;

namespace WebAppArchitecture.Base.Data;

/// <summary>SQL-translatable DTO projection bound to a specific database model.</summary>
public interface IEntityProjection<TDBModel, TDTO> : IEntityDTOModel
    where TDBModel : IEntityDBModel
    where TDTO : IEntityProjection<TDBModel, TDTO>
{
    static abstract Expression<Func<TDBModel, TDTO>> Projection { get; }
}
