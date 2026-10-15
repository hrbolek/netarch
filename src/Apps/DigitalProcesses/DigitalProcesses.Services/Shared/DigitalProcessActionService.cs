using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;

namespace WebAppArchitecture.DigitalProcesses.Services.Shared;
public interface IDigitalProcessActionService<TEntity> where TEntity : class, ISecuredEntityDBModel
{
    Task<bool> CanReadAsync(Guid id, CancellationToken ct = default);
}
public sealed class DigitalProcessActionService<TEntity>(IBaseService<TEntity> entities)
    : IDigitalProcessActionService<TEntity>
    where TEntity : class, ISecuredEntityDBModel
{
    public async Task<bool> CanReadAsync(Guid id, CancellationToken ct = default)
        => await entities.GetByIdAsync<EntityReadReference<TEntity>>(id, ct) is not null;
}

internal sealed record EntityReadReference<TEntity>(Guid id)
    : IEntityProjection<TEntity, EntityReadReference<TEntity>>
    where TEntity : class, ISecuredEntityDBModel
{
    public static Expression<Func<TEntity, EntityReadReference<TEntity>>> Projection => entity => new(entity.id);
}
