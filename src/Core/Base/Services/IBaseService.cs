using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Base.Services;

/// <summary>DTO-independent secured service contract. Every DTO must support this model.</summary>
public interface IBaseService<TDBModel> : ISecuredEntityService<TDBModel>
    where TDBModel : class, ISecuredEntityDBModel
{
    Task<IReadOnlyCollection<TDTO>> VectorAsync<TDTO>(Expression<Func<TDBModel, bool>> predicate, CancellationToken cancellationToken = default)
        where TDTO : IEntityDTOModel<TDBModel, TDTO>;
    Task<IReadOnlyCollection<TDTO>> VectorAsync<TDTO>(Expression<Func<TDBModel, bool>> predicate, string operation, CancellationToken cancellationToken = default)
        where TDTO : IEntityDTOModel<TDBModel, TDTO>;
    Task<IReadOnlyCollection<TDTO>> VectorAsync<TDTO>(Expression<Func<TDBModel, bool>> predicate, string operation, int? take, CancellationToken cancellationToken = default)
        where TDTO : IEntityDTOModel<TDBModel, TDTO>;
    Task<IReadOnlyCollection<TDTO>> VectorAsync<TDTO>(Expression<Func<TDBModel, bool>> predicate, string operation, VectorQueryOptions options, CancellationToken cancellationToken = default)
        where TDTO : IEntityDTOModel<TDBModel, TDTO>;
    Task<VectorPageResult<TDTO>> VectorPageAsync<TDTO>(Expression<Func<TDBModel, bool>> predicate, string operation, VectorQueryOptions options, CancellationToken cancellationToken = default)
        where TDTO : IEntityDTOModel<TDBModel, TDTO>;
}
