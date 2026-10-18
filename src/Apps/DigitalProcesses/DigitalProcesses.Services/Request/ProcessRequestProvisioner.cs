using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.RequestTransitionHistory;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.Request;
/// <summary>
/// Performs system-owned child creation only after the actor was authorized on
/// an existing target (Start) or Request (Transition). The capability is limited
/// to the Request subtree and only lives within an active transaction.
/// Generic AuthorizationLayer.StageCreateAsync is unchanged.
/// </summary>
/// <summary>
/// Vstupní kontrakt Process Request Provisioner. Přijímá pouze údaje od klienta; identita konajícího uživatele a bezpečnostní kontext se ověřují na serveru.
/// </summary>
public sealed class ProcessRequestProvisioner(IDBLayer db, IAuthorizationLayer security) : IProcessRequestProvisioner
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: IsAllowedAsync, CurrentUserId, GetByIdAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<IProcessRequestWriteScope> BeginStartAsync(Guid targetGroupId, GroupModel requestGroup, CancellationToken ct = default)
    {
        RequireTransaction();
        if (targetGroupId == Guid.Empty || requestGroup.id == Guid.Empty ||
            requestGroup.id == targetGroupId || requestGroup.master_group_id != targetGroupId ||
            requestGroup.rbacobject_id != requestGroup.id || requestGroup.inherit_roles ||
            !await security.IsAllowedAsync(targetGroupId, SecurityOperations.StartProcess, ct))
            throw new UnauthorizedAccessException("Cannot provision a request outside an authorized target group.");
        var target = await db.GetByIdAsync<GroupModel>(targetGroupId, ct)
            ?? throw new InvalidOperationException("Target group no longer exists.");
        if (!target.valid || requestGroup.path != ChildPath(target.path, requestGroup.id))
            throw new UnauthorizedAccessException("Invalid request-group ancestry.");
        var writer = new Writer(db, requestGroup.id, security.CurrentUserId);
        await writer.StageRequestRootAsync(requestGroup, ct);
        return writer;
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: IsAllowedAsync, CurrentUserId, GetByIdAsync, HasActiveTransaction; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<IProcessRequestWriteScope> BeginTransitionAsync(Guid requestGroupId, CancellationToken ct = default)
    {
        RequireTransaction();
        if (requestGroupId == Guid.Empty ||
            !await security.IsAllowedAsync(requestGroupId, SecurityOperations.Update, ct))
            throw new UnauthorizedAccessException("Request scope is not editable.");
        var group = await db.GetByIdAsync<GroupModel>(requestGroupId, ct)
            ?? throw new InvalidOperationException("Request group is missing.");
        if (group.rbacobject_id != group.id || group.inherit_roles)
            throw new UnauthorizedAccessException("Invalid Request RBAC boundary.");
        return new Writer(db, requestGroupId, security.CurrentUserId);
    }

    private void RequireTransaction()
    {
        if (!db.HasActiveTransaction)
            throw new InvalidOperationException("Request provisioning requires an active transaction.");
    }

    private static string ChildPath(string parentPath, Guid childId)
        => parentPath.TrimEnd('/') + $"/{childId:D}/";

    private sealed class Writer(IDBLayer db, Guid requestScopeId, Guid actor) : IProcessRequestWriteScope
    {
        private readonly Dictionary<Guid, string> newGroups = new();

        /// <summary>
        /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu. Volání: StageCreateAsync, HasActiveTransaction; pořadí a parametry viz implementace.
        /// </summary>
        public async Task StageRequestRootAsync(GroupModel group, CancellationToken ct)
        {
            newGroups.Add(group.id, group.path);
            await db.StageCreateAsync(group, ct);
        }

        public Task<TEntity> StageCreateAsync<TEntity>(TEntity entity, CancellationToken ct = default)
            where TEntity : class, ISecuredEntityDBModel
        {
            if (!db.HasActiveTransaction)
                throw new InvalidOperationException("Request provisioning requires an active transaction.");
            if (entity.created_by != actor || entity.updated_by != actor)
                throw new UnauthorizedAccessException("Provisioned entities must be attributed to the current actor.");
            if (entity is GroupModel group)
            {
                if (group.master_group_id is not Guid parent ||
                    !(parent == requestScopeId || newGroups.ContainsKey(parent)) ||
                    group.id == requestScopeId || group.rbacobject_id != group.id ||
                    group.path != ChildPath(GetParentPath(parent), group.id))
                    throw new UnauthorizedAccessException("Group must be a new descendant of this Request.");
                newGroups.Add(group.id, group.path);
            }
            else if (entity is RoleModel role)
            {
                if (role.group_id != requestScopeId || role.rbacobject_id != requestScopeId ||
                    role.user_id != actor || role.is_deputy)
                    throw new UnauthorizedAccessException("Only the applicant role may be provisioned.");
            }
            else if (entity is RequestModel request)
            {
                if (request.id != requestScopeId || request.rbacobject_id != requestScopeId ||
                    request.initiator_user_id != actor)
                    throw new UnauthorizedAccessException("Request does not match its provisioned boundary.");
            }
            else if (entity is SubmissionModel submission)
            {
                if (submission.request_id != requestScopeId || !newGroups.ContainsKey(submission.rbacobject_id))
                    throw new UnauthorizedAccessException("Submission must belong to the provisioned Request.");
            }
            else if (entity is SubmissionNodeModel node)
            {
                if (!(node.rbacobject_id == requestScopeId || newGroups.ContainsKey(node.rbacobject_id)))
                    throw new UnauthorizedAccessException("SubmissionNode is outside the provisioned subtree.");
            }
            else if (entity is RbacDenyRuleModel deny)
            {
                if (!newGroups.ContainsKey(deny.group_id) || deny.rbacobject_id != deny.group_id)
                    throw new UnauthorizedAccessException("Deny must target a new child boundary.");
            }
            else if (entity is RequestTransitionHistoryModel history)
            {
                if (history.request_id != requestScopeId || history.rbacobject_id != requestScopeId)
                    throw new UnauthorizedAccessException("History must belong to the Request.");
            }
            else
                throw new UnauthorizedAccessException("This entity type is not allowed in Request provisioning.");
            return db.StageCreateAsync(entity, ct);
        }

        private string GetParentPath(Guid parent)
        {
            if (newGroups.TryGetValue(parent, out var path)) return path;
            // In transition the persisted Request root is known to the writer.
            return db.Query<GroupModel>().Single(x => x.id == parent).path;
        }
    }
}
