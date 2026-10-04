using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

public sealed class SecurityProjectionService(IDBLayer db) : ISecurityProjectionService
{
    /// <summary>
    /// Rebuilds the materialized EffectiveAccess projection atomically.
    ///
    /// Runtime authorization stays deliberately simple: secured entity.rbacobject_id is
    /// matched to EffectiveAccess.rbacobject_id. All derivation complexity belongs here.
    ///
    /// Rules:
    /// - a Role over a Group applies to the Group and its descendants;
    /// - User.rbacobject_id == User.id;
    /// - a Role over a Group also applies to users whose Membership belongs to the
    ///   affected Group subtree;
    /// - user-object access is valid only for the intersection of Role and Membership
    ///   validity intervals.
    /// </summary>
    public Task RebuildAllAsync(CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(async ct =>
        {
            await db.StageDeleteWhereAsync<EffectiveAccessModel>(_ => true, ct);
            // Flush deletes before adding rows that may reuse the same composite keys.
            // This SaveChanges is still inside the same database transaction.
            await db.SaveChangesAsync(ct);

            var roles = db.Query<RoleModel>().ToArray();
            var groups = db.Query<GroupModel>()
                .Select(x => new { x.id, x.path })
                .ToArray();
            var memberships = db.Query<MembershipModel>()
                .Select(x => new
                {
                    x.id,
                    x.group_id,
                    x.user_id,
                    x.valid_from,
                    x.valid_to
                })
                .ToArray();

            var rows = new List<EffectiveAccessModel>();

            foreach (var role in roles)
            {
                var marker = $"/{role.group_id:D}/";
                var affectedGroupIds = groups
                    .Where(g => g.path.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    .Select(g => g.id)
                    .ToHashSet();

                // Group security objects.
                foreach (var targetGroupId in affectedGroupIds)
                {
                    rows.Add(new EffectiveAccessModel(
                        role.user_id,
                        targetGroupId,
                        role.role_type_id,
                        role.id,
                        role.group_id,
                        Guid.Empty,
                        role.valid_from.ToUnixTimeMilliseconds(),
                        role.valid_to?.ToUnixTimeMilliseconds()));
                }

                // User security objects. User.rbacobject_id == User.id.
                foreach (var membership in memberships.Where(m => affectedGroupIds.Contains(m.group_id)))
                {
                    var validFrom = role.valid_from >= membership.valid_from
                        ? role.valid_from
                        : membership.valid_from;

                    var validTo = MinNullable(role.valid_to, membership.valid_to);
                    if (validTo is not null && validTo.Value < validFrom)
                        continue;

                    rows.Add(new EffectiveAccessModel(
                        role.user_id,
                        membership.user_id,
                        role.role_type_id,
                        role.id,
                        membership.group_id,
                        membership.id,
                        validFrom.ToUnixTimeMilliseconds(),
                        validTo?.ToUnixTimeMilliseconds()));
                }
            }

            if (rows.Count > 0)
                await db.StageCreateRangeAsync(rows, ct);

            await db.SaveChangesAsync(ct);
        }, cancellationToken);

    private static DateTimeOffset? MinNullable(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (left is null)
            return right;
        if (right is null)
            return left;
        return left <= right ? left : right;
    }
}
