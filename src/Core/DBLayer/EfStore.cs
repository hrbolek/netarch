using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

public abstract class EfStore<TModel> : IBaseStore<TModel> where TModel : class, IEntityDBModel
{
    protected readonly ApplicationDbContext Db;
    protected DbSet<TModel> Set => Db.Set<TModel>();
    protected IQueryable<TModel> RawQuery => Set.AsNoTracking();

    public IQueryable<TModel> Query() => RawQuery;

    protected EfStore(ApplicationDbContext db) => Db = db;

    public virtual Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => RawQuery.SingleOrDefaultAsync(x => x.id == id, cancellationToken);

    public virtual async Task<IReadOnlyCollection<TModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => await RawQuery.ToArrayAsync(cancellationToken);

    public virtual async Task<IReadOnlyCollection<TModel>> VectorAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default)
        => await RawQuery.Where(predicate).ToArrayAsync(cancellationToken);

    public virtual Task<TModel> CreateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        Set.Add(model);
        return Task.FromResult(model);
    }

    public virtual Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        Set.Update(model);
        return Task.FromResult(model);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await Set.FindAsync([id], cancellationToken);
        if (model is null) return;
        Set.Remove(model);
    }
}
