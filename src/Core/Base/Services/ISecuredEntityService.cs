using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Base.Services;

/// <summary>DTO-independent public contract for secured entity access.</summary>
public interface ISecuredEntityService<TDBModel>
    where TDBModel : class, ISecuredEntityDBModel
{
    IQueryable<TDTO> Query<TDTO>() where TDTO : IEntityProjection<TDBModel, TDTO>;
    Task<TDTO?> GetByIdAsync<TDTO>(Guid id, CancellationToken cancellationToken = default)
        where TDTO : IEntityProjection<TDBModel, TDTO>;
    Task<IReadOnlyCollection<TDTO>> GetAllAsync<TDTO>(CancellationToken cancellationToken = default)
        where TDTO : IEntityProjection<TDBModel, TDTO>;
    IQueryable<TDTO> Where<TDTO>(Expression<Func<TDBModel, bool>> predicate, string operation = "list")
        where TDTO : IEntityProjection<TDBModel, TDTO>;
    Task<TDTO> UpdateAsync<TUpdateRequest, TDTO>(TUpdateRequest model, CancellationToken cancellationToken = default)
        where TUpdateRequest : IUpdateDTOModel<TDBModel>
        where TDTO : IEntityMapper<TDBModel, TDTO>;
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
