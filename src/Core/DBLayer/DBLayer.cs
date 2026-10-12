using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Typ D B Layer ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
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
        // Records returned by AsNoTracking can share a key with an instance
        // still tracked from a previous write in this unit of work. Update the
        // tracked instance instead of attaching a second copy with the same key.
        var key = db.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey();
        if (key is not null)
        {
            var entityEntry = db.Entry(entity);
            var local = db.ChangeTracker.Entries<TEntity>().FirstOrDefault(tracked =>
                tracked.State != EntityState.Detached &&
                key.Properties.All(property => Equals(
                    tracked.Property(property.Name).CurrentValue,
                    entityEntry.Property(property.Name).CurrentValue)));
            if (local is not null)
            {
                local.CurrentValues.SetValues(entity);
                return entity;
            }
        }
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

    public Task<int> DeleteWhereAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class, IEntityDBModel
        => db.Set<TEntity>().Where(predicate).ExecuteDeleteAsync(cancellationToken);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: SaveChangesAsync, Database; pořadí a parametry viz implementace.
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: Database; pořadí a parametry viz implementace.
    /// </summary>
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
