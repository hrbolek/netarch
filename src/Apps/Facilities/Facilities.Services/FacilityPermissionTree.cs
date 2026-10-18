using System.Collections.ObjectModel;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Facilities;

/// <summary>
/// Private-command persistence boundary for owned permission groups. The caller owns
/// the Facilities advisory lock and transaction; ordinary CRUD must not use this helper.
/// </summary>
public sealed class FacilityPermissionTree(
    IAuthorizationLayer security,
    IDBLayer db,
    CoreDbContext context,
    ISecurityProjectionService projection)
{
    public const string MoveOperation = "move";
    private Guid? lockedTransactionId;

    /// <summary>Acquire before reading any move/create source or checking authorization.</summary>
    public async Task LockAsync(CancellationToken ct = default)
    {
        var transaction = context.Database.CurrentTransaction;
        if (!context.Database.IsNpgsql() || transaction is null ||
            !security.HasActiveTransaction || !db.HasActiveTransaction ||
            transaction.GetDbTransaction().IsolationLevel != IsolationLevel.ReadCommitted)
            throw new InvalidOperationException(
                "Facility permission commands require a shared ReadCommitted PostgreSQL transaction.");

        if (lockedTransactionId == transaction.TransactionId) return;

        // Core writers do not take the Facilities advisory lock. Table locks also
        // stabilize grants, deny rules and projections while the copy is authorized.
        await context.Database.ExecuteSqlRawAsync(
            """
            LOCK TABLE groups, roles, memberships, rbac_access_bindings, rbac_deny_rules,
                group_types, role_types, operations, role_type_operations, effective_access
            IN SHARE ROW EXCLUSIVE MODE
            """, ct);
        lockedTransactionId = transaction.TransactionId;
    }

    public async Task<GroupModel> CreateOwnedGroupAsync(
        string name, Guid parentGroupId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        await LockAsync(ct);
        var parent = await RequireGroupAsync(parentGroupId, SecurityOperations.Create, ct);
        var actor = security.CurrentUserId;
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var group = new GroupModel(id, name, parent.group_type_id, parent.id,
            $"{parent.path}{id:D}/", true, id, now, now, actor, actor);
        var created = await db.StageAuditedCreateAsync(group, actor, ct);
        await security.SaveChangesAsync(ct);
        await projection.SynchronizeGroupSubtreeAsync(created.id, ct);
        return created;
    }

    /// <summary>
    /// Copies security source data, invokes the caller to rebind every mapped Facility
    /// and update physical paths, then saves and projects within the caller's transaction.
    /// No original groups, grants or memberships are changed.
    /// </summary>
    public async Task<T> CopyForMoveAsync<T>(
        Guid sourceGroupId,
        Guid destinationGroupId,
        Func<IReadOnlyDictionary<Guid, Guid>, CancellationToken, Task<T>> rebindCallback,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rebindCallback);
        await LockAsync(ct);
        var source = await RequireGroupAsync(sourceGroupId, MoveOperation, ct);
        var destination = await RequireGroupAsync(destinationGroupId, SecurityOperations.Create, ct);
        var groups = await db.Query<GroupModel>()
            .Where(x => x.path.StartsWith(source.path))
            .OrderBy(x => x.path.Length).ThenBy(x => x.id).ToListAsync(ct);
        var groupIds = groups.Select(x => x.id).ToArray();
        if (groupIds.Contains(destination.id))
            throw new InvalidOperationException("The destination cannot be inside the source permission subtree.");

        if (await db.Query<RbacAccessBindingModel>().AnyAsync(x =>
                groupIds.Contains(x.source_group_id) || groupIds.Contains(x.target_rbacobject_id) ||
                groupIds.Contains(x.rbacobject_id) ||
                (x.managed_scope_id.HasValue && groupIds.Contains(x.managed_scope_id.Value)), ct))
            throw new InvalidOperationException(
                "Permission subtrees with access bindings cannot be copied safely.");

        var roles = await db.Query<RoleModel>().Where(x => groupIds.Contains(x.group_id)).ToListAsync(ct);
        var memberships = await db.Query<MembershipModel>()
            .Where(x => groupIds.Contains(x.group_id)).ToListAsync(ct);
        var denies = await db.Query<RbacDenyRuleModel>()
            .Where(x => groupIds.Contains(x.group_id)).ToListAsync(ct);
        var roleIds = roles.Select(x => x.id).ToArray();
        var roleIdSet = roleIds.ToHashSet();
        if (roles.Any(x => x.is_deputy &&
                (!x.delegated_by_role_id.HasValue || !roleIdSet.Contains(x.delegated_by_role_id.Value))) ||
            roles.Any(x => x.delegated_by_role_id.HasValue &&
                !roleIdSet.Contains(x.delegated_by_role_id.Value)) ||
            await db.Query<RoleModel>().AnyAsync(x =>
                !groupIds.Contains(x.group_id) && x.delegated_by_role_id.HasValue &&
                roleIds.Contains(x.delegated_by_role_id.Value), ct))
            throw new InvalidOperationException(
                "Permission subtrees with external or missing deputy dependencies cannot be copied.");

        if (roles.Any(x => x.rbacobject_id != x.group_id) ||
            memberships.Any(x => x.rbacobject_id != x.group_id) ||
            denies.Any(x => x.rbacobject_id != x.group_id))
            throw new InvalidOperationException(
                "Permission assignments must be secured by their owning group before copying.");

        var sourceGroups = groups.ToDictionary(x => x.id);
        var groupMap = groups.ToDictionary(x => x.id, _ => Guid.NewGuid());
        var roleMap = roles.ToDictionary(x => x.id, _ => Guid.NewGuid());
        var copiedGroups = new Dictionary<Guid, GroupModel>();
        var actor = security.CurrentUserId;
        var now = DateTimeOffset.UtcNow;

        foreach (var group in groups)
        {
            GroupModel parent;
            if (group.id == source.id)
                parent = destination;
            else if (group.master_group_id is Guid parentId && copiedGroups.TryGetValue(parentId, out var copiedParent))
                parent = copiedParent;
            else
                throw new InvalidOperationException("The source permission subtree has inconsistent parent paths.");

            if (group.rbacobject_id != group.id || string.IsNullOrWhiteSpace(group.path))
                throw new InvalidOperationException("Permission groups must own distinct, valid scopes.");
            if (group.id != source.id &&
                (group.master_group_id is not Guid originalParentId ||
                 !sourceGroups.TryGetValue(originalParentId, out var originalParent) ||
                 group.path != $"{originalParent.path}{group.id:D}/"))
                throw new InvalidOperationException("The source permission subtree has inconsistent parent paths.");
            var id = groupMap[group.id];
            var copy = new GroupModel(id, group.Name, group.group_type_id, parent.id,
                $"{parent.path}{id:D}/", group.id == source.id || group.inherit_roles,
                id, now, now, actor, actor) { valid = group.valid };
            copiedGroups.Add(group.id, await db.StageAuditedCreateAsync(copy, actor, ct));
        }

        foreach (var role in roles)
        {
            var groupId = groupMap[role.group_id];
            var copy = new RoleModel(roleMap[role.id], groupId, role.user_id, role.role_type_id,
                role.is_deputy, role.valid_from, role.valid_to,
                role.delegated_by_role_id is Guid delegatedId ? roleMap[delegatedId] : null,
                groupId, now, now, actor, actor);
            await db.StageAuditedCreateAsync(copy, actor, ct);
        }

        foreach (var membership in memberships)
        {
            var groupId = groupMap[membership.group_id];
            await db.StageAuditedCreateAsync(new MembershipModel(Guid.NewGuid(), groupId,
                membership.user_id, membership.valid_from, membership.valid_to,
                groupId, now, now, actor, actor), actor, ct);
        }

        foreach (var deny in denies)
        {
            var groupId = groupMap[deny.group_id];
            await db.StageAuditedCreateAsync(new RbacDenyRuleModel(Guid.NewGuid(), groupId,
                deny.role_type_id, deny.include_descendants, groupId, now, now, actor, actor), actor, ct);
        }

        var result = await rebindCallback(new ReadOnlyDictionary<Guid, Guid>(groupMap), ct);
        await security.SaveChangesAsync(ct);
        await projection.SynchronizeGroupSubtreeAsync(groupMap[source.id], ct);
        return result;
    }

    private async Task<GroupModel> RequireGroupAsync(Guid id, string operation, CancellationToken ct)
    {
        if (id == Guid.Empty || !await security.IsAllowedAsync(id, operation, ct))
            throw new UnauthorizedAccessException("The permission group is unavailable for this operation.");
        var group = await db.Query<GroupModel>().SingleOrDefaultAsync(x => x.id == id, ct);
        if (group is null || group.rbacobject_id != group.id ||
            !group.path.EndsWith($"/{group.id:D}/", StringComparison.Ordinal))
            throw new InvalidOperationException("The permission group is missing or has an invalid scope/path.");
        return group;
    }
}
