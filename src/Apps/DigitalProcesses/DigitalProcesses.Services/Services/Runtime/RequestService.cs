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
    ISecurityProjectionService securityProjection)
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

        // ProcessDefinition.rbacobject_id is an existing RBAC Group below e.g.
        // FacultyRequestsRBACGroup. Request gets its own inheritance boundary below it.
        var processGroup = Security.Query<GroupModel>(SecurityOperations.Create)
            .SingleOrDefault(x => x.id == process.rbacobject_id)
            ?? throw new InvalidOperationException(
                "ProcessDefinition.rbacobject_id must reference a Group on which the caller may create requests.");

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
            await Security.StageCreateAsync(requestGroup, token);
            await Security.StageCreateAsync(submissionGroup, token);

            // Initiator identity is anchored on Request, not in form values.
            await Security.StageCreateAsync(new RoleModel(
                Guid.NewGuid(), requestGroup.id, actor, process.initiator_role_type_id,
                false, now, null, null, requestGroup.id, now, now, actor, actor), token);

            await Security.StageCreateAsync(request, token);
            await Security.StageCreateAsync(submission, token);

            var instantiated = await InstantiateMinimumTreeAsync(
                formNodes, submission, submissionGroup, requestGroup, actor, now, token);

            await ApplyStateAccessRulesAsync(
                initial.id, request, submission, instantiated, now, actor, token);

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

        var oldNodes = Security.Query<SubmissionNodeModel>(SecurityOperations.Read)
            .Where(x => x.submission_id == currentSubmission.id)
            .OrderBy(x => x.path).ToArray();
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

            await Security.StageCreateAsync(newSubmissionGroup, token);
            await Security.StageCreateAsync(newSubmission, token);

            var cloned = await CloneSubmissionTreeAsync(
                oldNodes, definitions, newSubmission, newSubmissionGroup,
                requestGroup, actor, now, token);

            await ApplyStateAccessRulesAsync(
                transition.target_state_id, currentRequest, newSubmission, cloned,
                now, actor, token);

            var history = new RequestTransitionHistoryModel(
                Guid.NewGuid(), currentRequest.id, transition.id,
                currentRequest.current_state_id, transition.target_state_id,
                currentSubmission.id, actor, r.note, now,
                currentRequest.rbacobject_id, now, now, actor, actor);
            await Security.StageCreateAsync(history, token);

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

    private async Task<SubmissionNodeModel[]> InstantiateMinimumTreeAsync(
        FormNodeModel[] definitions,
        SubmissionModel submission,
        GroupModel submissionGroup,
        GroupModel requestGroup,
        Guid actor,
        DateTimeOffset now,
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

                    if (def.own_security_scope)
                    {
                        securityGroup = NewBoundaryGroup(
                            Guid.NewGuid(), $"{def.key} [{occurrence}]", inheritedSecurityGroup, actor, now);
                        await Security.StageCreateAsync(securityGroup, ct);
                    }

                    var node = new SubmissionNodeModel(
                        id, submission.id, def.id, submissionParent?.id, path,
                        occurrence, def.position, null, securityGroup.id,
                        now, now, actor, actor);
                    await Security.StageCreateAsync(node, ct);
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

            if (def.own_security_scope)
            {
                securityGroup = NewBoundaryGroup(
                    Guid.NewGuid(), $"{def.key} [{old.occurrence}]", inheritedSecurityGroup, actor, now);
                await Security.StageCreateAsync(securityGroup, ct);
            }

            var clone = new SubmissionNodeModel(
                id, newSubmission.id, old.form_node_id, parent?.id, path,
                old.occurrence, old.position, old.value_json, securityGroup.id,
                now, now, actor, actor);
            await Security.StageCreateAsync(clone, ct);
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
