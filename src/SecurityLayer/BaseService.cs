using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;

namespace WebAppArchitecture.SecurityLayer;

public class BaseService<TDBModel, TDTOModel> : IBaseService<TDBModel, TDTOModel>
    where TDBModel : class, ISecuredEntityDBModel
    where TDTOModel : IEntityDTOModel<TDBModel, TDTOModel>
{
    protected readonly ISecurityLayer Security;
    public BaseService(ISecurityLayer security) => Security = security;

    public virtual IQueryable<TDTOModel> Query() => Security.Query<TDBModel>().Select(TDTOModel.Projection);
    public virtual Task<TDTOModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Query().SingleOrDefault(x => x.id == id));
    public virtual Task<IReadOnlyCollection<TDTOModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<TDTOModel>>(Query().ToArray());
    public virtual Task<IReadOnlyCollection<TDTOModel>> VectorAsync(Expression<Func<TDBModel, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<TDTOModel>>(Security.Query<TDBModel>().Where(predicate).Select(TDTOModel.Projection).ToArray());
    public virtual Task<TDTOModel> CreateAsync<TCreateRequest>(TCreateRequest model, CancellationToken cancellationToken = default) where TCreateRequest : ICreateDTOModel<TDBModel>
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            var entity = await Security.StageCreateAsync(model.ToModel(), ct);
            await Security.SaveChangesAsync(ct);
            return TDTOModel.FromModel(entity);
        }, cancellationToken);
    public virtual async Task<TDTOModel> UpdateAsync<TUpdateRequest>(TUpdateRequest model, CancellationToken cancellationToken = default) where TUpdateRequest : IUpdateDTOModel<TDBModel>
    {
        var current = await Security.GetByIdAsync<TDBModel>(model.id, SecurityOperations.Update, cancellationToken)
            ?? throw new InvalidOperationException($"Item with id '{model.id}' does not exist or is not accessible.");
        return await Security.ExecuteInTransactionAsync(async ct =>
        {
            var updated = await Security.StageUpdateAsync(model.ApplyTo(current), ct);
            await Security.SaveChangesAsync(ct);
            return TDTOModel.FromModel(updated);
        }, cancellationToken);
    }
    public virtual Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Security.ExecuteInTransactionAsync(async ct =>
        {
            await Security.StageDeleteAsync<TDBModel>(id, ct);
            await Security.SaveChangesAsync(ct);
        }, cancellationToken);
}
