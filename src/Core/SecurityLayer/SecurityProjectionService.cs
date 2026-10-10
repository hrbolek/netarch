using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Maintains the materialized EffectiveAccess projection.
/// Runtime mutations use impact-based synchronization. Full rebuild is reserved for
/// bootstrap/restore/repair scenarios.
/// </summary>
public sealed class SecurityProjectionService(IDBLayer db) : ISecurityProjectionService
{
    public Task RebuildAllAsync(CancellationToken cancellationToken = default)
        => db.ExecuteInTransactionAsync(async ct =>
        {
            // Projection rebuild is a replacement operation, not an entity-by-entity
            // update. Delete directly in SQL inside the same transaction before
            // inserting the freshly calculated projection. This avoids EF change
            // tracker ordering issues when composite keys are reused.
            await db.DeleteWhereAsync<EffectiveAccessModel>(_ => true, ct);
            var desired = BuildDesiredRows(null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (desired.Length > 0)
                await db.StageCreateRangeAsync(desired, ct);
            await db.SaveChangesAsync(ct);
        }, cancellationToken);

    public Task SynchronizeRbacObjectAsync(Guid rbacObjectId, CancellationToken cancellationToken = default)
        => SynchronizeRbacObjectsAsync([rbacObjectId], cancellationToken);

    public Task SynchronizeRbacObjectsAsync(IEnumerable<Guid> rbacObjectIds, CancellationToken cancellationToken = default)
    {
        var ids = rbacObjectIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        return ids.Length == 0 ? Task.CompletedTask : SynchronizeTargetsAsync(ids, cancellationToken);
    }

    /// <summary>
    /// Synchronizes all RBAC objects that can be affected by roles rooted in the
    /// supplied groups: group descendants and exact targets of functional access bindings
    /// sourced from those descendants. Membership never affects roles or EffectiveAccess.
    /// </summary>
    public Task SynchronizeRoleImpactAsync(IEnumerable<Guid> sourceGroupIds, CancellationToken cancellationToken = default)
    {
        var roots = sourceGroupIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (roots.Length == 0) return Task.CompletedTask;
        var impacted = GetSubtreeImpactRbacObjectIds(roots);
        return SynchronizeRbacObjectsAsync(impacted, cancellationToken);
    }

    /// <summary>
    /// Synchronizes one group subtree and all User/binding targets whose effective
    /// rights can be changed by that subtree.
    /// </summary>
    public Task SynchronizeGroupSubtreeAsync(Guid rootGroupId, CancellationToken cancellationToken = default)
        => SynchronizeRoleImpactAsync([rootGroupId], cancellationToken);

    private Guid[] GetSubtreeImpactRbacObjectIds(IReadOnlyCollection<Guid> rootGroupIds)
    {
        var descendantIds = new HashSet<Guid>(rootGroupIds);

        // Materialized paths let us resolve only the affected branches. Runtime
        // changes normally have one root (Role/Group/Request), so a small number of
        // indexed/filtered branch queries is preferable to loading the whole tree.
        foreach (var root in rootGroupIds)
        {
            var token = $"/{root:D}/";
            var branchIds = db.Query<GroupModel>()
                .Where(x => x.path.Contains(token))
                .Select(x => x.id)
                .ToArray();
            descendantIds.UnionWith(branchIds);
        }

        var ids = descendantIds.ToArray();
        var result = new HashSet<Guid>(descendantIds);

        if (ids.Length > 0)
        {
            var bindingTargets = db.Query<RbacAccessBindingModel>()
                .Where(x => ids.Contains(x.source_group_id))
                .Select(x => x.target_rbacobject_id)
                .ToArray();
            result.UnionWith(bindingTargets);
        }

        return result.ToArray();
    }

    private Task SynchronizeTargetsAsync(Guid[] targetIds, CancellationToken cancellationToken)
        => db.ExecuteInTransactionAsync(async ct =>
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var targetSet = targetIds.ToHashSet();
            var desired = BuildDesiredRows(targetSet, nowMs);
            var current = db.Query<EffectiveAccessModel>()
                .Where(x => targetIds.Contains(x.rbacobject_id))
                .ToArray();

            var desiredByKey = desired.ToDictionary(KeyOf);
            var currentByKey = current.ToDictionary(KeyOf);

            // Expire rows that are no longer desired. They remain physically present
            // for a short retention window and are removed by background maintenance.
            foreach (var existing in current)
            {
                var key = KeyOf(existing);
                if (desiredByKey.ContainsKey(key))
                    continue;
                if (existing.valid_to_unix_ms is not null && existing.valid_to_unix_ms < nowMs)
                    continue;

                await db.StageUpdateAsync(existing with { valid_to_unix_ms = nowMs - 1 }, ct);
            }

            foreach (var wanted in desired)
            {
                var key = KeyOf(wanted);
                if (!currentByKey.TryGetValue(key, out var existing))
                {
                    await db.StageCreateAsync(wanted, ct);
                    continue;
                }

                if (!Equivalent(existing, wanted))
                    await db.StageUpdateAsync(wanted, ct);
            }

            await db.SaveChangesAsync(ct);
        }, cancellationToken);

    private EffectiveAccessModel[] BuildDesiredRows(IReadOnlySet<Guid>? targetRbacObjectIds, long nowMs)
    {
        if (targetRbacObjectIds is null)
            return BuildDesiredRowsFull(nowMs);

        // Targeted runtime synchronization: load only security sources that can
        // contribute to the requested RBAC objects.
        var targetIds = targetRbacObjectIds.ToArray();
        var targetGroups = db.Query<GroupModel>()
            .Where(x => targetIds.Contains(x.id))
            .Select(x => new GroupProjection(x.id, x.path, x.inherit_roles))
            .ToArray();

        var targetBindings = db.Query<RbacAccessBindingModel>()
            .Where(x => targetIds.Contains(x.target_rbacobject_id))
            .ToArray();

        var referencedGroupIds = targetBindings
            .Select(x => x.source_group_id)
            .Distinct()
            .ToArray();

        var referencedGroups = referencedGroupIds.Length == 0
            ? Array.Empty<GroupProjection>()
            : db.Query<GroupModel>()
                .Where(x => referencedGroupIds.Contains(x.id))
                .Select(x => new GroupProjection(x.id, x.path, x.inherit_roles))
                .ToArray();

        var ancestorIds = targetGroups
            .Concat(referencedGroups)
            .SelectMany(x => ParsePath(x.Path))
            .Distinct()
            .ToArray();

        var ancestorGroups = ancestorIds.Length == 0
            ? Array.Empty<GroupProjection>()
            : db.Query<GroupModel>()
                .Where(x => ancestorIds.Contains(x.id))
                .Select(x => new GroupProjection(x.id, x.path, x.inherit_roles))
                .ToArray();

        var groupById = targetGroups
            .Concat(referencedGroups)
            .Concat(ancestorGroups)
            .DistinctBy(x => x.Id)
            .ToDictionary(x => x.Id);

        var roles = ancestorIds.Length == 0
            ? Array.Empty<RoleModel>()
            : db.Query<RoleModel>()
                .Where(x => ancestorIds.Contains(x.group_id))
                .ToArray();

        var denies = LoadRelevantDenyRules(groupById.Values);
        return CalculateRows(
            roles,
            targetGroups,
            groupById,
            targetBindings,
            targetRbacObjectIds,
            nowMs, denies);
    }

    private EffectiveAccessModel[] BuildDesiredRowsFull(long nowMs)
    {
        var groups = db.Query<GroupModel>()
            .Select(x => new GroupProjection(x.id, x.path, x.inherit_roles))
            .ToArray();
        var groupById = groups.ToDictionary(x => x.Id);
        var roles = db.Query<RoleModel>().ToArray();
        var bindings = db.Query<RbacAccessBindingModel>().ToArray();
        var denies = LoadRelevantDenyRules(groups);
        return CalculateRows(roles, groups, groupById, bindings, null, nowMs, denies);
    }

    private RbacDenyRuleModel[] LoadRelevantDenyRules(IEnumerable<GroupProjection> groups)
    {
        var ids = groups.Select(x => x.Id).Distinct().ToArray();
        return ids.Length == 0 ? [] : db.Query<RbacDenyRuleModel>()
            .Where(x => ids.Contains(x.group_id)).ToArray();
    }

    private static EffectiveAccessModel[] CalculateRows(
        IReadOnlyCollection<RoleModel> roles,
        IReadOnlyCollection<GroupProjection> candidateTargetGroups,
        IReadOnlyDictionary<Guid, GroupProjection> groupById,
        IReadOnlyCollection<RbacAccessBindingModel> bindings,
        IReadOnlySet<Guid>? targetRbacObjectIds,
        long nowMs, IReadOnlyCollection<RbacDenyRuleModel> denyRules)
    {
        var rows = new List<EffectiveAccessModel>();
        bool Wants(Guid rbacObjectId) => targetRbacObjectIds is null || targetRbacObjectIds.Contains(rbacObjectId);

        foreach (var role in roles)
        {
            if (!groupById.TryGetValue(role.group_id, out var sourceGroup)) continue;
            if (role.valid_to is not null && role.valid_to.Value.ToUnixTimeMilliseconds() < nowMs) continue;

            var affectedGroupIds = candidateTargetGroups
                .Where(target => RoleFlowsToTarget(sourceGroup, target, groupById))
                .Select(x => x.Id)
                .ToHashSet();

            foreach (var targetGroupId in affectedGroupIds)
            {
                if (!Wants(targetGroupId)) continue;
                rows.Add(new EffectiveAccessModel(
                    role.user_id, targetGroupId, role.role_type_id,
                    role.id, role.group_id, Guid.Empty, Guid.Empty,
                    role.valid_from.ToUnixTimeMilliseconds(), role.valid_to?.ToUnixTimeMilliseconds()));
            }


        }

        foreach (var binding in bindings)
        {
            if (!Wants(binding.target_rbacobject_id)) continue;
            if (binding.valid_to is not null && binding.valid_to.Value.ToUnixTimeMilliseconds() < nowMs) continue;
            if (!groupById.TryGetValue(binding.source_group_id, out var sourceScope)) continue;

            foreach (var role in roles.Where(r => r.role_type_id == binding.source_role_type_id))
            {
                if (!PathContainsGroup(sourceScope.Path, role.group_id)) continue;

                var validFrom = Max(role.valid_from, binding.valid_from);
                var validTo = MinNullable(role.valid_to, binding.valid_to);
                if (validTo is not null && validTo.Value < validFrom) continue;
                if (validTo is not null && validTo.Value.ToUnixTimeMilliseconds() < nowMs) continue;

                rows.Add(new EffectiveAccessModel(
                    role.user_id, binding.target_rbacobject_id, binding.target_role_type_id,
                    role.id, binding.source_group_id, Guid.Empty, binding.id,
                    validFrom.ToUnixTimeMilliseconds(), validTo?.ToUnixTimeMilliseconds()));
            }
        }

        // A binding may map Applicant -> Reader. Match the source role as well as
        // the projected target role, without denying Reader to the Dean.
        var sourceRoleTypes = roles.ToDictionary(x => x.id, x => x.role_type_id);
        bool IsDenied(EffectiveAccessModel row)
        {
            if (!groupById.TryGetValue(row.rbacobject_id, out var target)) return false;
            return denyRules.Any(rule =>
                (rule.role_type_id == row.role_type_id ||
                 (sourceRoleTypes.TryGetValue(row.source_role_id, out var sourceRoleType) &&
                  sourceRoleType == rule.role_type_id)) &&
                (rule.group_id == target.Id ||
                 (rule.include_descendants && PathContainsGroup(target.Path, rule.group_id))));
        }
        return rows
            .Where(row => !IsDenied(row))
            .GroupBy(KeyOf)
            .Select(g => g.OrderByDescending(x => x.valid_from_unix_ms).First())
            .ToArray();
    }

    private static EffectiveAccessKey KeyOf(EffectiveAccessModel x) => new(
        x.user_id, x.rbacobject_id, x.role_type_id,
        x.source_role_id, x.source_membership_id, x.source_access_binding_id);

    private static bool Equivalent(EffectiveAccessModel left, EffectiveAccessModel right)
        => left.source_group_id == right.source_group_id
            && left.valid_from_unix_ms == right.valid_from_unix_ms
            && left.valid_to_unix_ms == right.valid_to_unix_ms;

    private static bool RoleFlowsToTarget(GroupProjection source, GroupProjection target, IReadOnlyDictionary<Guid, GroupProjection> groupById)
    {
        if (!PathContainsGroup(target.Path, source.Id)) return false;
        if (source.Id == target.Id) return true;

        var ids = ParsePath(target.Path);
        var sourceIndex = Array.IndexOf(ids, source.Id);
        if (sourceIndex < 0) return false;

        for (var i = sourceIndex + 1; i < ids.Length; i++)
            if (groupById.TryGetValue(ids[i], out var group) && !group.InheritRoles)
                return false;

        return true;
    }

    private static bool PathContainsGroup(string path, Guid groupId)
        => path.Contains($"/{groupId:D}/", StringComparison.OrdinalIgnoreCase);

    private static Guid[] ParsePath(string path)
        => path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToArray();

    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right) => left >= right ? left : right;
    private static DateTimeOffset? MinNullable(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (left is null) return right;
        if (right is null) return left;
        return left <= right ? left : right;
    }

    private readonly record struct EffectiveAccessKey(
        Guid UserId,
        Guid RbacObjectId,
        Guid RoleTypeId,
        Guid SourceRoleId,
        Guid SourceMembershipId,
        Guid SourceAccessBindingId);

    private sealed record GroupProjection(Guid Id, string Path, bool InheritRoles);
}
