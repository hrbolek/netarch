using System.Text.Json;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed class RequestService(
    ISecurityLayer security,
    IDigitalProcessIntegration integration,
    IRbacAccessBindingManager bindingManager,
    ISecurityProjectionService securityProjection,
    ITrustedSubmissionSnapshotReader snapshots,
    IProcessRequestProvisioner provisioner)
    : BaseService<RequestModel, RequestResponse>(security), IRequestService
{
    public async Task<RequestResponse> StartAsync(StartRequestRequest r, CancellationToken ct = default)
    {
        var definition = await Security.GetByIdAsync<ProcessDefinitionVersionModel>(
            r.process_definition_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Process definition version not found.");
        if (!definition.is_published)
            throw new InvalidOperationException("Requests can only start from a published process definition version.");

        var process = await Security.GetByIdAsync<ProcessDefinitionModel>(
            definition.process_definition_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Process definition not found.");

        // The published version fixes WHERE new requests may be started.
        // Group validity and type are checked against the actual DB row, never user-supplied data.
        if (r.target_group_id is not Guid targetId || targetId == Guid.Empty)
            throw new InvalidOperationException("TargetGroupId must be specified.");
        if (definition.target_group_type_id is not Guid permittedType || permittedType == Guid.Empty)
            throw new InvalidOperationException("Published process version has no TargetGroupTypeId.");

        // Starting a process is a separate operation, not general Create on the faculty.
        var processGroup = Security.Query<GroupModel>(SecurityOperations.StartProcess)
            .SingleOrDefault(x => x.id == targetId && x.valid && x.group_type_id == permittedType)
            ?? throw new InvalidOperationException(
                "Target group is absent, retired, has the wrong type, or caller lacks start_process.");

        var initial = Security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .SingleOrDefault(x => x.workflow_version_id == definition.workflow_version_id && x.is_initial)
            ?? throw new InvalidOperationException("Workflow must contain exactly one visible initial state.");

        var formNodes = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == definition.form_version_id)
            .OrderBy(x => x.path).ThenBy(x => x.position).ToArray();

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var requestId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();

        var requestGroup = NewBoundaryGroup(
            requestId, $"Request {requestId:D}", processGroup, actor, now);
        var submissionGroup = NewBoundaryGroup(
            submissionId, $"Submission 1", requestGroup, actor, now);

        var request = new RequestModel(
            requestId, definition.id, actor, initial.id, submissionId, r.subject,
            requestGroup.id, now, now, actor, actor);
        var submission = new SubmissionModel(
            submissionId, requestId, definition.form_version_id, 1, false, null,
            submissionGroup.id, now, now, actor, actor);

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var writer = await provisioner.BeginStartAsync(processGroup.id, requestGroup, token);
            await writer.StageCreateAsync(submissionGroup, token);

            // Initiator identity is anchored on Request, not in form values.
            await writer.StageCreateAsync(new RoleModel(
                Guid.NewGuid(), requestGroup.id, actor, process.initiator_role_type_id,
                false, now, null, null, requestGroup.id, now, now, actor, actor), token);

            await writer.StageCreateAsync(request, token);
            await writer.StageCreateAsync(submission, token);

            var policy = LoadApplicantPolicy(definition.id, initial.id, formNodes);
            var instantiated = await InstantiateMinimumTreeAsync(
                formNodes, submission, submissionGroup, requestGroup, actor, now, policy, writer, token);

            await ApplyStateAccessRulesAsync(
                initial.id, request, submission, instantiated, now, actor, token);
            await ApplyApplicantAccessAsync(policy, process.initiator_role_type_id,
                request, submission, instantiated, actor, now, writer, token);

            await Security.SaveChangesAsync(token);
            await securityProjection.SynchronizeGroupSubtreeAsync(requestGroup.id, token);
            return RequestResponse.FromModel(request);
        }, ct);
    }

    public async Task<RequestResponse> TransitionAsync(
        Guid requestId,
        TransitionRequestRequest r,
        CancellationToken ct = default)
    {
        var currentRequest = await Security.GetByIdAsync<RequestModel>(
            requestId, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Request not found or not editable.");

        var transition = await Security.GetByIdAsync<WorkflowTransitionModel>(
            r.transition_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Transition not found.");
        if (transition.source_state_id != currentRequest.current_state_id)
            throw new InvalidOperationException("Transition does not start in the request current state.");
        if (transition.note_required && string.IsNullOrWhiteSpace(r.note))
            throw new InvalidOperationException("This transition requires a note.");

        var currentSubmission = await Security.GetByIdAsync<SubmissionModel>(
            currentRequest.current_submission_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Active submission not found or not editable.");
        if (currentSubmission.is_archived)
            throw new InvalidOperationException("Current submission is already archived.");

        var requestGroup = Security.Query<GroupModel>(SecurityOperations.Update)
            .SingleOrDefault(x => x.id == currentRequest.rbacobject_id)
            ?? throw new InvalidOperationException("Request RBAC group is missing or not editable.");

        // Authorized Request transition is checked above. A trusted copy MUST retain
        // data hidden from the actor's read view, otherwise archival loses values.
        var oldNodes = snapshots.ReadCompleteTree(currentSubmission.id);
        var definitions = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == currentSubmission.form_version_id)
            .ToDictionary(x => x.id);

        if (!string.IsNullOrWhiteSpace(transition.condition_query))
        {
            var result = await integration.ExecuteAsync(
                transition.condition_query, SerializeSubmission(oldNodes), ct);
            if (!IsTruthy(result))
                throw new InvalidOperationException("Workflow transition condition rejected the transition.");
        }

        var processVersion = await Security.GetByIdAsync<ProcessDefinitionVersionModel>(
            currentRequest.process_definition_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Process version not found.");
        var process = await Security.GetByIdAsync<ProcessDefinitionModel>(
            processVersion.process_definition_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Process not found.");
        var applicantPolicy = LoadApplicantPolicy(processVersion.id, transition.target_state_id, definitions.Values);

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var newSubmissionId = Guid.NewGuid();
        var newSubmissionGroup = NewBoundaryGroup(
            newSubmissionId, $"Submission {currentSubmission.version + 1}", requestGroup, actor, now);
        var newSubmission = new SubmissionModel(
            newSubmissionId, currentRequest.id, currentSubmission.form_version_id,
            currentSubmission.version + 1, false, null, newSubmissionGroup.id,
            now, now, actor, actor);

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            await Security.StageUpdateAsync(currentSubmission with
            {
                is_archived = true,
                archived_at = now,
                updated_at = now,
                updated_by = actor
            }, token);

            // Request-target bindings represent the ACTIVE state's request permissions,
            // so they expire when leaving a state. Historical Submission bindings stay.
            await bindingManager.ExpireManagedBindingsAsync(
                currentRequest.id, currentRequest.rbacobject_id, now, actor, token);

            var writer = await provisioner.BeginTransitionAsync(requestGroup.id, token);
            await writer.StageCreateAsync(newSubmissionGroup, token);
            await writer.StageCreateAsync(newSubmission, token);

            var cloned = await CloneSubmissionTreeAsync(
                oldNodes, definitions, newSubmission, newSubmissionGroup,
                requestGroup, actor, now, applicantPolicy, writer, token);

            await ApplyStateAccessRulesAsync(
                transition.target_state_id, currentRequest, newSubmission, cloned,
                now, actor, token);

            await ApplyApplicantAccessAsync(applicantPolicy, process.initiator_role_type_id,
                currentRequest, newSubmission, cloned, actor, now, writer, token);

            var history = new RequestTransitionHistoryModel(
                Guid.NewGuid(), currentRequest.id, transition.id,
                currentRequest.current_state_id, transition.target_state_id,
                currentSubmission.id, actor, r.note, now,
                currentRequest.rbacobject_id, now, now, actor, actor);
            await writer.StageCreateAsync(history, token);

            var updatedRequest = currentRequest with
            {
                current_state_id = transition.target_state_id,
                current_submission_id = newSubmissionId,
                updated_at = now,
                updated_by = actor
            };
            await Security.StageUpdateAsync(updatedRequest, token);

            await Security.SaveChangesAsync(token);
            await securityProjection.SynchronizeGroupSubtreeAsync(requestGroup.id, token);

            if (!string.IsNullOrWhiteSpace(transition.action_query))
                await integration.ExecuteAsync(
                    transition.action_query, SerializeSubmission(cloned), token);

            return RequestResponse.FromModel(updatedRequest);
        }, ct);
    }

    public async Task<RequestDetailResponse> GetDetailAsync(Guid requestId, CancellationToken ct = default)
    {
        var r = await Security.GetByIdAsync<RequestModel>(requestId, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException();
        var state = await Security.GetByIdAsync<WorkflowStateModel>(r.current_state_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException();
        var submission = await Security.GetByIdAsync<SubmissionModel>(r.current_submission_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException();
        var nodes = Security.Query<SubmissionNodeModel>(SecurityOperations.Read)
            .Where(x => x.submission_id == submission.id)
            .OrderBy(x => x.path)
            .Select(SubmissionNodeResponse.Projection).ToArray();
        var history = Security.Query<RequestTransitionHistoryModel>(SecurityOperations.Read)
            .Where(x => x.request_id == requestId)
            .Select(RequestTransitionHistoryResponse.Projection).ToArray()
            .OrderBy(x => x.changed_at).ToArray();
        return new(RequestResponse.FromModel(r), WorkflowStateResponse.FromModel(state),
            new(SubmissionResponse.FromModel(submission), nodes), history);
    }

    // An explicit applicant policy is opt-in at the process version/state level.
    // Nodes without a rule inherit their parent policy; when no policy exists, legacy behavior stays unchanged.
    private Dictionary<Guid, bool> LoadApplicantPolicy(Guid processVersionId, Guid stateId, IEnumerable<FormNodeModel> nodes)
    {
        var items = nodes.ToArray();
        var configured = Security.Query<FormNodeApplicantAccessModel>(SecurityOperations.Read)
            .Where(x => x.process_definition_version_id == processVersionId && x.workflow_state_id == stateId)
            .ToDictionary(x => x.form_node_id, x => x.can_read);
        if (configured.Count == 0) return configured;
        // Fail closed at top-level: when any policy is configured, only explicitly allowed branches are visible.
        var output = new Dictionary<Guid, bool>();
        foreach (var node in items.OrderBy(x => x.path))
        {
            bool inherited = node.parent_id is Guid pid && output.TryGetValue(pid, out var parentAllowed) && parentAllowed;
            var hasOwnRule = configured.TryGetValue(node.id, out var ownAllowed);
            output[node.id] = node.parent_id is null
                ? hasOwnRule && ownAllowed
                : inherited && (!hasOwnRule || ownAllowed);
        }
        return output;
    }

    private async Task ApplyApplicantAccessAsync(
        IReadOnlyDictionary<Guid, bool> policy, Guid applicantRoleTypeId,
        RequestModel request, SubmissionModel submission,
        IReadOnlyCollection<SubmissionNodeModel> nodes, Guid actor, DateTimeOffset now, IProcessRequestWriteScope writer, CancellationToken ct)
    {
        // Applicant's role is anchored on Request; the Submission Group is a
        // role-inheritance boundary. Create explicit bindings to accessible scopes.
        // Bindings are scoped to this Submission and retained on archive.
        var grantedGroups = nodes
            .Where(x => policy.Count == 0 ||
                (policy.TryGetValue(x.form_node_id, out var canRead) && canRead))
            .Select(x => x.rbacobject_id)
            .Append(submission.rbacobject_id)
            .Distinct().ToArray();
        foreach (var groupId in grantedGroups)
            await bindingManager.StageCreateAsync(new RbacAccessBindingModel(
                Guid.NewGuid(), request.rbacobject_id, applicantRoleTypeId,
                groupId, applicantRoleTypeId, now, null, submission.id,
                groupId, now, now, actor, actor), ct);

        if (policy.Count == 0) return;
        // Only the first hidden group in a branch needs a persistent Deny.
        // Its descendants are filtered by their materialized RBAC paths.
        var deniedGroupIds = nodes
            .Where(x => policy.TryGetValue(x.form_node_id, out var canRead) && !canRead)
            .Select(x => x.rbacobject_id).Distinct().ToArray();
        foreach (var groupId in deniedGroupIds)
            await writer.StageCreateAsync(new RbacDenyRuleModel(
                Guid.NewGuid(), groupId, applicantRoleTypeId, true,
                groupId, now, now, actor, actor), ct);
    }

    private static bool IsDeniedBoundary(FormNodeModel node,
        IReadOnlyDictionary<Guid, bool> policy, IEnumerable<FormNodeModel> definitions)
    {
        if (!policy.TryGetValue(node.id, out var allowed) || allowed) return false;
        return node.parent_id is null ||
            !policy.TryGetValue(node.parent_id.Value, out var parentAllowed) || parentAllowed;
    }

    private async Task<SubmissionNodeModel[]> InstantiateMinimumTreeAsync(
        FormNodeModel[] definitions,
        SubmissionModel submission,
        GroupModel submissionGroup,
        GroupModel requestGroup,
        Guid actor,
        DateTimeOffset now,
        IReadOnlyDictionary<Guid, bool> applicantPolicy,
        IProcessRequestWriteScope writer,
        CancellationToken ct)
    {
        var result = new List<SubmissionNodeModel>();

        async Task AddChildren(Guid? formParent, SubmissionNodeModel? submissionParent, GroupModel inheritedSecurityGroup)
        {
            foreach (var def in definitions.Where(x => x.parent_id == formParent).OrderBy(x => x.position))
            {
                for (var occurrence = 0; occurrence < def.min_occurs; occurrence++)
                {
                    var id = Guid.NewGuid();
                    var path = submissionParent is null
                        ? MaterializedPath.Root(id)
                        : MaterializedPath.Child(submissionParent.path, id);
                    var securityGroup = inheritedSecurityGroup;

                    if (def.own_security_scope || IsDeniedBoundary(def, applicantPolicy, definitions))
                    {
                        securityGroup = NewBoundaryGroup(
                            Guid.NewGuid(), $"{def.key} [{occurrence}]", inheritedSecurityGroup, actor, now);
                        if (!def.own_security_scope) securityGroup = securityGroup with { inherit_roles = true };
                        await writer.StageCreateAsync(securityGroup, ct);
                    }

                    var node = new SubmissionNodeModel(
                        id, submission.id, def.id, submissionParent?.id, path,
                        occurrence, def.position, null, securityGroup.id,
                        now, now, actor, actor);
                    await writer.StageCreateAsync(node, ct);
                    result.Add(node);
                    await AddChildren(def.id, node, securityGroup);
                }
            }
        }

        await AddChildren(null, null, submissionGroup);
        return result.ToArray();
    }

    private async Task<SubmissionNodeModel[]> CloneSubmissionTreeAsync(
        SubmissionNodeModel[] oldNodes,
        IReadOnlyDictionary<Guid, FormNodeModel> definitions,
        SubmissionModel newSubmission,
        GroupModel newSubmissionGroup,
        GroupModel requestGroup,
        Guid actor,
        DateTimeOffset now,
        IReadOnlyDictionary<Guid, bool> applicantPolicy,
        IProcessRequestWriteScope writer,
        CancellationToken ct)
    {
        var clones = new List<SubmissionNodeModel>();
        var idMap = new Dictionary<Guid, SubmissionNodeModel>();
        var securityGroupByOldNode = new Dictionary<Guid, GroupModel>();

        foreach (var old in oldNodes.OrderBy(x => x.path))
        {
            if (!definitions.TryGetValue(old.form_node_id, out var def))
                throw new InvalidOperationException($"FormNode {old.form_node_id} is missing from immutable form version.");

            var id = Guid.NewGuid();
            SubmissionNodeModel? parent = old.parent_id is Guid oldParentId ? idMap[oldParentId] : null;
            var inheritedSecurityGroup = old.parent_id is Guid parentId
                ? securityGroupByOldNode[parentId]
                : newSubmissionGroup;
            var securityGroup = inheritedSecurityGroup;
            var path = parent is null ? MaterializedPath.Root(id) : MaterializedPath.Child(parent.path, id);

            if (def.own_security_scope || IsDeniedBoundary(def, applicantPolicy, definitions.Values))
            {
                securityGroup = NewBoundaryGroup(
                    Guid.NewGuid(), $"{def.key} [{old.occurrence}]", inheritedSecurityGroup, actor, now);
                if (!def.own_security_scope) securityGroup = securityGroup with { inherit_roles = true };
                await writer.StageCreateAsync(securityGroup, ct);
            }

            var clone = new SubmissionNodeModel(
                id, newSubmission.id, old.form_node_id, parent?.id, path,
                old.occurrence, old.position, old.value_json, securityGroup.id,
                now, now, actor, actor);
            await writer.StageCreateAsync(clone, ct);
            idMap[old.id] = clone;
            securityGroupByOldNode[old.id] = securityGroup;
            clones.Add(clone);
        }

        return clones.ToArray();
    }

    private async Task ApplyStateAccessRulesAsync(
        Guid stateId,
        RequestModel request,
        SubmissionModel submission,
        IReadOnlyCollection<SubmissionNodeModel> nodes,
        DateTimeOffset now,
        Guid actor,
        CancellationToken ct)
    {
        var rules = Security.Query<WorkflowStateAccessRuleModel>(SecurityOperations.Read)
            .Where(x => x.workflow_state_id == stateId)
            .ToArray();

        foreach (var rule in rules)
        {
            IEnumerable<Guid> targets = rule.target_scope switch
            {
                WorkflowAccessTargetScopes.Request => [request.rbacobject_id],
                WorkflowAccessTargetScopes.Submission => [submission.rbacobject_id],
                WorkflowAccessTargetScopes.FormNode when rule.target_form_node_id is Guid formNodeId
                    => nodes.Where(x => x.form_node_id == formNodeId)
                        .Select(x => x.rbacobject_id).Distinct(),
                _ => throw new InvalidOperationException($"Unsupported access target scope '{rule.target_scope}'.")
            };

            foreach (var target in targets.Distinct())
            {
                var managedScopeId = rule.target_scope == WorkflowAccessTargetScopes.Request
                    ? request.id
                    : submission.id;

                await bindingManager.StageCreateAsync(new RbacAccessBindingModel(
                    Guid.NewGuid(),
                    request.rbacobject_id, // source: all functional roles visible at this Request scope
                    rule.source_role_type_id,
                    target,
                    rule.target_role_type_id,
                    now,
                    null,
                    managedScopeId,
                    target,
                    now,
                    now,
                    actor,
                    actor), ct);
            }
        }
    }

    private static GroupModel NewBoundaryGroup(
        Guid id,
        string name,
        GroupModel parent,
        Guid actor,
        DateTimeOffset now)
        => new(
            id,
            name,
            parent.group_type_id,
            parent.id,
            parent.path.TrimEnd('/') + $"/{id:D}/",
            false,
            id,
            now,
            now,
            actor,
            actor);

    private static string SerializeSubmission(IEnumerable<SubmissionNodeModel> nodes)
        => JsonSerializer.Serialize(nodes.Select(x => new
        {
            x.id,
            x.form_node_id,
            x.parent_id,
            x.occurrence,
            x.value_json
        }));

    private static bool IsTruthy(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var d = JsonDocument.Parse(json);
            return d.RootElement.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.Number => d.RootElement.TryGetInt64(out var n) && n != 0,
                JsonValueKind.String => bool.TryParse(d.RootElement.GetString(), out var b) && b,
                _ => false
            };
        }
        catch { return false; }
    }
}
