using System.Data;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.Events;

public sealed record StandardTypeImportResult(int Inserted, int Existing, int Total);
public static class EventTypeImport
{
    public static async Task<StandardTypeImportResult?> RunAsync(
        Guid actor, Guid scopeId, CoreDbContext db, SecurityService security, CancellationToken ct)
    {
        if (scopeId == Guid.Empty || !await security.IsAllowedAsync(actor, scopeId, "create", ct))
            return null;

        // Unique indexes on both ID (PK) and Code guard against duplicate inserts.
        // Serializable transaction ensures a concurrent import cannot silently duplicate rows.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var ids = StandardEventTypes.All.Select(x => x.Id).ToArray();
        var codes = StandardEventTypes.All.Select(x => x.Code).ToArray();
        var existing = await db.Set<EventTypeModel>()
            .AsNoTracking().Where(x => ids.Contains(x.id) || codes.Contains(x.Code))
            .Select(x => new { x.id, x.Code }).ToListAsync(ct);
        var existingIds = existing.Select(x => x.id).ToHashSet();
        var existingCodes = existing.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
        var now = DateTimeOffset.UtcNow;
        var inserted = 0;
        foreach (var definition in StandardEventTypes.All)
        {
            if (existingIds.Contains(definition.Id) || existingCodes.Contains(definition.Code)) continue;
            db.Set<EventTypeModel>().Add(new EventTypeModel
            {
                id = definition.Id,
                Code = definition.Code,
                Name = definition.Name,
                Abbreviation = definition.Abbreviation,
                Color = definition.Color,
                RegistrationMode = "inherit",
                rbacobject_id = scopeId,
                created_by = actor,
                updated_by = actor,
                created_at = now,
                updated_at = now
            });
            inserted++;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new StandardTypeImportResult(inserted, StandardEventTypes.All.Count - inserted, StandardEventTypes.All.Count);
    }
}

public sealed record StandardTypeImportRequest(Guid RbacObjectId);
