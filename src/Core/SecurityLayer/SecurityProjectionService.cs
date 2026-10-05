using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

public sealed class SecurityProjectionService(IDBLayer db) : ISecurityProjectionService
{
    /// <summary>
    /// Rebuilds the only runtime authorization projection used by SecurityLayer.
    ///
    /// Sources:
    /// 1. direct Role assignments projected through the Group tree;
    /// 2. Membership projection to User security objects;
    /// 3. RbacAccessBinding: a functional role visible in a source scope is mapped
    ///    to an operational role on one exact target RBAC object.
    ///
    /// Group.inherit_roles=false is an inheritance boundary for ordinary Role projection.
    /// A role directly assigned on such a group still applies to that group, but roles from
    /// ancestors do not cross the boundary. AccessBinding source resolution intentionally
    /// inspects the ancestor path independently of this boundary: bindings use roles as
    /// functional selectors, not as inherited target permissions.
    /// </summary>
    public Task RebuildAllAsync(CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(async ct =>
        {
            await db.StageDeleteWhereAsync<EffectiveAccessModel>(_ => true, ct);
            await db.SaveChangesAsync(ct);

            var roles = db.Query<RoleModel>().ToArray();
            var groups = db.Query<GroupModel>()
                .Select(x => new GroupProjection(x.id, x.path, x.inherit_roles))
                .ToArray();
            var groupById = groups.ToDictionary(x => x.Id);
            var memberships = db.Query<MembershipModel>()
                .Select(x => new MembershipProjection(
                    x.id, x.group_id, x.user_id, x.valid_from, x.valid_to))
                .ToArray();
            var bindings = db.Query<RbacAccessBindingModel>().ToArray();

            var rows = new List<EffectiveAccessModel>();

            // ------------------------------------------------------------
            // Direct Role -> Group tree + Membership/User projection.
            // ------------------------------------------------------------
            foreach (var role in roles)
            {
                if (!groupById.TryGetValue(role.group_id, out var sourceGroup))
                    continue;

                var affectedGroupIds = groups
                    .Where(target => RoleFlowsToTarget(sourceGroup, target, groupById))
                    .Select(x => x.Id)
                    .ToHashSet();

                foreach (var targetGroupId in affectedGroupIds)
                {
                    rows.Add(new EffectiveAccessModel(
                        role.user_id,
                        targetGroupId,
                        role.role_type_id,
                        role.id,
                        role.group_id,
                        Guid.Empty,
                        Guid.Empty,
                        role.valid_from.ToUnixTimeMilliseconds(),
                        role.valid_to?.ToUnixTimeMilliseconds()));
                }

                // User.rbacobject_id == User.id. Membership inherits only when the
                // role itself legitimately reaches the membership group.
                foreach (var membership in memberships.Where(m => affectedGroupIds.Contains(m.GroupId)))
                {
                    var validFrom = Max(role.valid_from, membership.ValidFrom);
                    var validTo = MinNullable(role.valid_to, membership.ValidTo);
                    if (validTo is not null && validTo.Value < validFrom)
                        continue;

                    rows.Add(new EffectiveAccessModel(
                        role.user_id,
                        membership.UserId,
                        role.role_type_id,
                        role.id,
                        membership.GroupId,
                        membership.Id,
                        Guid.Empty,
                        validFrom.ToUnixTimeMilliseconds(),
                        validTo?.ToUnixTimeMilliseconds()));
                }
            }

            // ------------------------------------------------------------
            // Functional binding projection.
            //
            // A binding is intentionally exact-target. It does not inherit from
            // target_rbacobject_id to Group descendants. Digital Processes creates
            // explicit bindings for each security scope that must be visible.
            // This makes hidden/isolated FormNode scopes possible without a deny ACL.
            // ------------------------------------------------------------
            foreach (var binding in bindings)
            {
                if (!groupById.TryGetValue(binding.source_group_id, out var sourceScope))
                    continue;

                foreach (var role in roles.Where(r => r.role_type_id == binding.source_role_type_id))
                {
                    // A functional role qualifies when its assigned group is the
                    // source scope itself or any ancestor of that scope. We ignore
                    // inherit_roles here on purpose: the binding is the explicit bridge.
                    if (!PathContainsGroup(sourceScope.Path, role.group_id))
                        continue;

                    var validFrom = Max(role.valid_from, binding.valid_from);
                    var validTo = MinNullable(role.valid_to, binding.valid_to);
                    if (validTo is not null && validTo.Value < validFrom)
                        continue;

                    rows.Add(new EffectiveAccessModel(
                        role.user_id,
                        binding.target_rbacobject_id,
                        binding.target_role_type_id,
                        role.id,
                        binding.source_group_id,
                        Guid.Empty,
                        binding.id,
                        validFrom.ToUnixTimeMilliseconds(),
                        validTo?.ToUnixTimeMilliseconds()));
                }
            }

            if (rows.Count > 0)
                await db.StageCreateRangeAsync(rows, ct);

            await db.SaveChangesAsync(ct);
        }, cancellationToken);

    private static bool RoleFlowsToTarget(
        GroupProjection source,
        GroupProjection target,
        IReadOnlyDictionary<Guid, GroupProjection> groupById)
    {
        if (!PathContainsGroup(target.Path, source.Id))
            return false;

        if (source.Id == target.Id)
            return true;

        var ids = ParsePath(target.Path);
        var sourceIndex = Array.IndexOf(ids, source.Id);
        if (sourceIndex < 0)
            return false;

        // A boundary blocks roles from every ancestor above it, including access
        // to the boundary group itself. Roles assigned directly on the boundary
        // group are evaluated separately with that group as the source.
        for (var i = sourceIndex + 1; i < ids.Length; i++)
        {
            if (groupById.TryGetValue(ids[i], out var group) && !group.InheritRoles)
                return false;
        }

        return true;
    }

    private static bool PathContainsGroup(string path, Guid groupId)
        => path.Contains($"/{groupId:D}/", StringComparison.OrdinalIgnoreCase);

    private static Guid[] ParsePath(string path)
        => path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Guid.Parse)
            .ToArray();

    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right)
        => left >= right ? left : right;

    private static DateTimeOffset? MinNullable(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (left is null) return right;
        if (right is null) return left;
        return left <= right ? left : right;
    }

    private sealed record GroupProjection(Guid Id, string Path, bool InheritRoles);
    private sealed record MembershipProjection(
        Guid Id,
        Guid GroupId,
        Guid UserId,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidTo);
}
