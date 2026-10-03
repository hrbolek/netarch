using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

public sealed class SecurityProjectionService(IDBLayer db) : ISecurityProjectionService
{
    /// <summary>
    /// Rebuilds the materialized EffectiveAccess projection atomically.
    /// If an outer transaction already exists (for example a Group/Role mutation),
    /// DBLayer reuses it; otherwise a transaction is created for this rebuild.
    /// </summary>
    public Task RebuildAllAsync(CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(async ct =>
        {
            await db.StageDeleteWhereAsync<EffectiveAccessModel>(_ => true, ct);
            // Flush deletes before adding rows that can have identical composite keys.
            // This SaveChanges is still inside the same database transaction.
            await db.SaveChangesAsync(ct);

            var roles = db.Query<RoleModel>().ToArray();
            var groups = db.Query<GroupModel>().Select(x => new { x.id, x.path }).ToArray();
            var rows = new List<EffectiveAccessModel>();

            foreach (var role in roles)
            {
                var marker = $"/{role.group_id:D}/";
                foreach (var target in groups.Where(g => g.path.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                    rows.Add(new EffectiveAccessModel(
                        role.user_id,
                        target.id,
                        role.role_type_id,
                        role.id,
                        role.group_id,
                        role.valid_from,
                        role.valid_to));
            }

            if (rows.Count > 0)
                await db.StageCreateRangeAsync(rows, ct);

            await db.SaveChangesAsync(ct);
        }, cancellationToken);
}
