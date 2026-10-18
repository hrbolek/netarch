using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Datová komponenta Ef Store. Připravuje dotazy a zápisy pro vyšší vrstvy bez vykreslování HTML a bez HTTP odpovědností.
/// </summary>
public abstract class EfStore<TModel> : IBaseStore<TModel> where TModel : class, IEntityDBModel
{
    protected readonly CoreDbContext Db;
    protected DbSet<TModel> Set => Db.Set<TModel>();
    protected IQueryable<TModel> RawQuery => Set.AsNoTracking();

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public IQueryable<TModel> Query() => RawQuery;

    protected EfStore(CoreDbContext db) => Db = db;

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce.
    /// </summary>
    public virtual Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => RawQuery.SingleOrDefaultAsync(x => x.id == id, cancellationToken);

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce.
    /// </summary>
    public virtual async Task<IReadOnlyCollection<TModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => await RawQuery.ToArrayAsync(cancellationToken);

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public virtual async Task<IReadOnlyCollection<TModel>> VectorAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default)
        => await RawQuery.Where(predicate).ToArrayAsync(cancellationToken);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách.
    /// </summary>
    public virtual Task<TModel> CreateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        Set.Add(model);
        return Task.FromResult(model);
    }

    /// <summary>
    /// Zpracovává změnu existující entity; ověření oprávnění a integrity musí probíhat ve službě, ne pouze v UI.
    /// </summary>
    public virtual Task<TModel> UpdateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        var local = Set.Local.SingleOrDefault(item => item.id == model.id);
        if (local is not null)
            Db.Entry(local).CurrentValues.SetValues(model);
        else
            Set.Update(model);
        return Task.FromResult(model);
    }

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě.
    /// </summary>
    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await Set.FindAsync([id], cancellationToken);
        if (model is null) return;
        Set.Remove(model);
    }
}
