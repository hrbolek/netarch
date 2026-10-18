using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Datová komponenta Ef Role Type Store. Připravuje dotazy a zápisy pro vyšší vrstvy bez vykreslování HTML a bez HTTP odpovědností.
/// </summary>
public sealed class EfRoleTypeStore(CoreDbContext db) : EfStore<RoleTypeModel>(db), IRoleTypeStore
{
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce.
    /// </summary>
    public override async Task<RoleTypeModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Db.RoleTypes.AsNoTracking()
            .Include(x => x.operation_links).ThenInclude(x => x.operation)
            .SingleOrDefaultAsync(x => x.id == id, cancellationToken);

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce.
    /// </summary>
    public override async Task<IReadOnlyCollection<RoleTypeModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Db.RoleTypes.AsNoTracking()
            .Include(x => x.operation_links).ThenInclude(x => x.operation)
            .ToArrayAsync(cancellationToken);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách.
    /// </summary>
    public override async Task<RoleTypeModel> CreateAsync(RoleTypeModel model, CancellationToken cancellationToken = default)
    {
        var clean = model with { operation_links = new List<RoleTypeOperationModel>() };
        Db.RoleTypes.Add(clean);
        await SyncOperationsAsync(clean.id, model.requested_operations, cancellationToken);
        return clean;
    }

    /// <summary>
    /// Zpracovává změnu existující entity; ověření oprávnění a integrity musí probíhat ve službě, ne pouze v UI.
    /// </summary>
    public override async Task<RoleTypeModel> UpdateAsync(RoleTypeModel model, CancellationToken cancellationToken = default)
    {
        var existing = await Db.RoleTypes.SingleAsync(x => x.id == model.id, cancellationToken);
        Db.Entry(existing).CurrentValues.SetValues(model);
        await SyncOperationsAsync(model.id, model.requested_operations, cancellationToken);
        return model;
    }

    private async Task SyncOperationsAsync(Guid roleTypeId, IEnumerable<string> operationNames, CancellationToken ct)
    {
        var names = operationNames.Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var operations = await Db.Operations.Where(x => names.Contains(x.name)).ToArrayAsync(ct);
        var missing = names.Except(operations.Select(x => x.name), StringComparer.OrdinalIgnoreCase).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"Unknown operations: {string.Join(", ", missing)}. Register operations before assigning them to a RoleType.");

        var current = await Db.RoleTypeOperations.Where(x => x.role_type_id == roleTypeId).ToArrayAsync(ct);
        Db.RoleTypeOperations.RemoveRange(current);
        foreach (var operation in operations)
            Db.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(), roleTypeId, operation.id));
    }
}
