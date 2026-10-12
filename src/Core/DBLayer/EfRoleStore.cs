using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Datová komponenta Ef Role Store. Připravuje dotazy a zápisy pro vyšší vrstvy bez vykreslování HTML a bez HTTP odpovědností.
/// </summary>
public sealed class EfRoleStore(ApplicationDbContext db) : EfStore<RoleModel>(db), IRoleStore
{
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce.
    /// </summary>
    public override async Task<RoleModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.Roles.AsNoTracking().Include(x => x.role_type).SingleOrDefaultAsync(x => x.id == id, cancellationToken);

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce.
    /// </summary>
    public override async Task<IReadOnlyCollection<RoleModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Db.Roles.AsNoTracking().Include(x => x.role_type).ToArrayAsync(cancellationToken);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách.
    /// </summary>
    public override Task<RoleModel> CreateAsync(RoleModel model, CancellationToken cancellationToken = default)
    {
        Db.Roles.Add(model);
        return Task.FromResult(model);
    }

    /// <summary>
    /// Zpracovává změnu existující entity; ověření oprávnění a integrity musí probíhat ve službě, ne pouze v UI.
    /// </summary>
    public override Task<RoleModel> UpdateAsync(RoleModel model, CancellationToken cancellationToken = default)
    {
        Db.Roles.Update(model);
        return Task.FromResult(model);
    }
}
