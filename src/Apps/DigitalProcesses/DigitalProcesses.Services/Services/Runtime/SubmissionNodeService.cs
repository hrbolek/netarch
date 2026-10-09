using System.Text.Json;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface ISubmissionNodeService : IBaseService<SubmissionNodeModel, SubmissionNodeResponse>
{
    Task<SubmissionNodeResponse> CreateInSubmissionAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default);
    Task<SubmissionNodeResponse> CreateInNodeAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default);
    Task<SubmissionNodeResponse> SetValueAsync(Guid id, SetSubmissionNodeValueRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<SubmissionNodeResponse>> GetSubtreeAsync(Guid id, CancellationToken ct = default);
}

public sealed class SubmissionNodeService(
    ISecurityLayer security,
    IRbacAccessBindingManager bindingManager,
    ISecurityProjectionService securityProjection)
    : BaseService<SubmissionNodeModel, SubmissionNodeResponse>(security), ISubmissionNodeService
{
    public Task<SubmissionNodeResponse> CreateInSubmissionAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default)
    {
        if (request.parent_id is not null)
            throw new InvalidOperationException("Root submission-node creation does not accept parent_id. Use create-in-node.");
        return CreateCoreAsync(request with { parent_id = null }, requireParent: false, ct);
    }

    public Task<SubmissionNodeResponse> CreateInNodeAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default)
    {
        if (request.parent_id is null)
            throw new InvalidOperationException("Child submission-node creation requires parent_id.");
        return CreateCoreAsync(request, requireParent: true, ct);
    }

    private async Task<SubmissionNodeResponse> CreateCoreAsync(CreateSubmissionNodeRequest r, bool requireParent, CancellationToken ct)
    {
        var submission = await Security.GetByIdAsync<SubmissionModel>(r.submission_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Submission not found.");
        if (submission.is_archived)
            throw new InvalidOperationException("Archived submission is immutable.");

        var formNode = await Security.GetByIdAsync<FormNodeModel>(r.form_node_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Form node not found.");
        if (formNode.form_version_id != submission.form_version_id)
            throw new InvalidOperationException("Form node belongs to another form version.");
        if (r.occurrence < 0 || r.occurrence >= formNode.max_occurs)
            throw new ArgumentOutOfRangeException(nameof(r.occurrence));

        SubmissionNodeModel? parent = null;
        if (requireParent && r.parent_id is null)
            throw new InvalidOperationException("Child submission-node creation requires parent_id.");
        if (!requireParent && r.parent_id is not null)
            throw new InvalidOperationException("Root submission-node creation does not accept parent_id.");

        if (r.parent_id is Guid pid)
        {
            (parent, _) = await CreationScopeGuard.RequireEntityAsync<SubmissionNodeModel>(
                Security, pid, "contained", ct);
            if (parent.submission_id != r.submission_id)
                throw new InvalidOperationException("Parent belongs to another submission.");
            if (formNode.parent_id != parent.form_node_id)
                throw new InvalidOperationException("Submission tree must mirror the form definition tree.");
        }
        else if (formNode.parent_id is not null)
        {
            throw new InvalidOperationException("Non-root form node requires corresponding parent submission node.");
        }
        else
        {
            _ = await CreationScopeGuard.RequireEntityAsync<SubmissionModel>(
                Security, submission.id, "contained", ct);
        }

        var request = await Security.GetByIdAsync<RequestModel>(submission.request_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Request not found.");
        var parentRbac = parent?.rbacobject_id ?? submission.rbacobject_id;
        var parentSecurityGroup = Security.Query<GroupModel>(SecurityOperations.Read)
            .SingleOrDefault(x => x.id == parentRbac)
            ?? throw new InvalidOperationException("Parent security group is missing.");

        var id = Guid.NewGuid();
        var path = parent is null ? MaterializedPath.Root(id) : MaterializedPath.Child(parent.path, id);
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var nodeRbac = parentRbac;
        GroupModel? ownScopeGroup = null;

        if (formNode.own_security_scope)
        {
            ownScopeGroup = NewBoundaryGroup(
                Guid.NewGuid(), $"{formNode.key} [{r.occurrence}]", parentSecurityGroup, actor, now);
            nodeRbac = ownScopeGroup.id;
        }

        var model = new SubmissionNodeModel(
            id, r.submission_id, r.form_node_id, r.parent_id, path,
            r.occurrence, r.position, r.value_json, nodeRbac,
            now, now, actor, actor);

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            if (ownScopeGroup is not null)
            {
                await Security.StageCreateAsync(ownScopeGroup, token);
                var rules = Security.Query<WorkflowStateAccessRuleModel>(SecurityOperations.Read)
                    .Where(x => x.workflow_state_id == request.current_state_id
                        && x.target_scope == WorkflowAccessTargetScopes.FormNode
                        && x.target_form_node_id == formNode.id)
                    .ToArray();

                foreach (var rule in rules)
                {
                    await bindingManager.StageCreateAsync(new RbacAccessBindingModel(
                        Guid.NewGuid(), request.rbacobject_id, rule.source_role_type_id,
                        ownScopeGroup.id, rule.target_role_type_id,
                        now, null, submission.id, ownScopeGroup.id,
                        now, now, actor, actor), token);
                }
            }

            var created = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            if (ownScopeGroup is not null)
                await securityProjection.SynchronizeRbacObjectAsync(ownScopeGroup.id, token);
            return SubmissionNodeResponse.FromModel(created);
        }, ct);
    }

    public async Task<SubmissionNodeResponse> SetValueAsync(Guid id, SetSubmissionNodeValueRequest r, CancellationToken ct = default)
    {
        var node = await Security.GetByIdAsync<SubmissionNodeModel>(id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException();
        var submission = await Security.GetByIdAsync<SubmissionModel>(node.submission_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException();
        if (submission.is_archived)
            throw new InvalidOperationException("Archived submission is immutable.");
        var formNode = await Security.GetByIdAsync<FormNodeModel>(node.form_node_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException();
        var type = await Security.GetByIdAsync<FormNodeTypeModel>(formNode.node_type_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException();
        if (!type.can_have_value)
            throw new InvalidOperationException("This form node type does not carry a value.");
        if (r.value_json is not null)
        {
            using var _ = JsonDocument.Parse(r.value_json);
        }

        var updated = node with
        {
            value_json = r.value_json,
            updated_at = DateTimeOffset.UtcNow,
            updated_by = Security.CurrentUserId
        };
        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var saved = await Security.StageUpdateAsync(updated, token);
            await Security.SaveChangesAsync(token);
            return SubmissionNodeResponse.FromModel(saved);
        }, ct);
    }

    public Task<IReadOnlyCollection<SubmissionNodeResponse>> GetSubtreeAsync(Guid id, CancellationToken ct = default)
    {
        var root = Security.Query<SubmissionNodeModel>(SecurityOperations.Read)
            .SingleOrDefault(x => x.id == id) ?? throw new KeyNotFoundException();
        IReadOnlyCollection<SubmissionNodeResponse> result = Security.Query<SubmissionNodeModel>(SecurityOperations.Read)
            .Where(x => x.submission_id == root.submission_id
                && (x.path == root.path || x.path.StartsWith(root.path + "/")))
            .OrderBy(x => x.path).ThenBy(x => x.position)
            .Select(SubmissionNodeResponse.Projection).ToArray();
        return Task.FromResult(result);
    }

    private static GroupModel NewBoundaryGroup(Guid id, string name, GroupModel parent, Guid actor, DateTimeOffset now)
        => new(
            id, name, parent.group_type_id, parent.id,
            parent.path.TrimEnd('/') + $"/{id:D}/",
            false, id, now, now, actor, actor);
}
