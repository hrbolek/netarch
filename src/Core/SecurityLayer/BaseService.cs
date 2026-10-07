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

    public virtual IQueryable<TDTOModel> Query() => Security.Query<TDBModel>(SecurityOperations.List).Select(TDTOModel.Projection);
    public virtual Task<TDTOModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(
            Security.Query<TDBModel>(SecurityOperations.Read)
                .Where(entity => entity.id == id)
                .Select(TDTOModel.Projection)
                .SingleOrDefault());
    public virtual Task<IReadOnlyCollection<TDTOModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<TDTOModel>>(
            Security.Query<TDBModel>(SecurityOperations.List)
                .Select(TDTOModel.Projection)
                .ToArray());
    public virtual Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        CancellationToken cancellationToken = default)
        => VectorAsync(predicate, SecurityOperations.List, cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        CancellationToken cancellationToken = default)
        => VectorAsync(predicate, operation, take: null, cancellationToken: cancellationToken);

    public virtual Task<IReadOnlyCollection<TDTOModel>> VectorAsync(
        Expression<Func<TDBModel, bool>> predicate,
        string operation,
        int? take,
        CancellationToken cancellationToken = default)
    {
        var query = Security.Query<TDBModel>(operation)
            .Where(predicate)
            .Select(TDTOModel.Projection);

        if (take is > 0)
            query = query.Take(take.Value);

        return Task.FromResult<IReadOnlyCollection<TDTOModel>>(query.ToArray());
    }
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
