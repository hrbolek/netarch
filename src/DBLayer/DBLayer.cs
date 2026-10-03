using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

public sealed class DBLayer(ApplicationDbContext db, IServiceProvider services) : IDBLayer
{
    public bool HasActiveTransaction => db.Database.CurrentTransaction is not null;

    public IQueryable<TEntity> Query<TEntity>() where TEntity : class, IEntityDBModel
        => db.Set<TEntity>().AsNoTracking();

    private IBaseStore<TEntity>? Store<TEntity>() where TEntity : class, IEntityDBModel
        => services.GetService<IBaseStore<TEntity>>();

    public async Task<TEntity?> GetByIdAsync<TEntity>(Guid id, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel
    {
        var store = Store<TEntity>();
        if (store is not null) return await store.GetByIdAsync(id, cancellationToken);
        return await db.Set<TEntity>().AsNoTracking().SingleOrDefaultAsync(x => x.id == id, cancellationToken);
    }

    public async Task<TEntity> StageCreateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel
    {
        var store = Store<TEntity>();
        if (store is not null) return await store.CreateAsync(entity, cancellationToken);
        db.Set<TEntity>().Add(entity);
        return entity;
    }

    public async Task<TEntity> StageUpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel
    {
        var store = Store<TEntity>();
        if (store is not null) return await store.UpdateAsync(entity, cancellationToken);
        db.Set<TEntity>().Update(entity);
        return entity;
    }

    public async Task StageDeleteAsync<TEntity>(Guid id, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel
    {
        var store = Store<TEntity>();
        if (store is not null) { await store.DeleteAsync(id, cancellationToken); return; }
        var entity = await db.Set<TEntity>().SingleOrDefaultAsync(x => x.id == id, cancellationToken);
        if (entity is not null) db.Set<TEntity>().Remove(entity);
    }

    public async Task StageCreateRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel
        => await db.Set<TEntity>().AddRangeAsync(entities, cancellationToken);

    public async Task StageDeleteWhereAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel
    {
        var entities = await db.Set<TEntity>().Where(predicate).ToArrayAsync(cancellationToken);
        if (entities.Length > 0) db.Set<TEntity>().RemoveRange(entities);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (db.Database.CurrentTransaction is not null) { await action(cancellationToken); return; }
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (db.Database.CurrentTransaction is not null) return await action(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
