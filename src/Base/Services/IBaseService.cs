using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Base.Services;

public interface IBaseService<TDBModel, TDTOModel>
    where TDBModel : class, ISecuredEntityDBModel
    where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
{
    IQueryable<TDTOModel> Query();
    Task<TDTOModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TDTOModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TDTOModel>> VectorAsync(Expression<Func<TDBModel, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TDTOModel> CreateAsync<TCreateRequest>(TCreateRequest model, CancellationToken cancellationToken = default) where TCreateRequest : ICreateDTOModel<TDBModel>;
    Task<TDTOModel> UpdateAsync<TUpdateRequest>(TUpdateRequest model, CancellationToken cancellationToken = default) where TUpdateRequest : IUpdateDTOModel<TDBModel>;
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
