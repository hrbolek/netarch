using WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.RequestTransitionHistory;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowStateAccessRule;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.WorkflowVersion;
public sealed partial class WorkflowVersionService
{
    public async Task<WorkflowDesignerResponse> ReplaceDefinitionAsync(
        Guid id, WorkflowGraphDefinition request, CancellationToken ct = default)
    {
        ValidateDefinition(request);
        await Security.ExecuteInTransactionAsync(async token =>
        {
            var version = await Security.GetByIdAsync<WorkflowVersionModel>(id, SecurityOperations.Update, token)
                ?? throw new KeyNotFoundException("Workflow version not found or not editable.");
            if (version.is_published) throw new InvalidOperationException("Published workflow version is immutable.");
            await ReplaceDefinitionCoreAsync(version, request, token);
        }, ct);
        return await GetDesignerAsync(id, ct);
    }

    private static void ValidateDefinition(WorkflowGraphDefinition request, bool publish = false)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.states is null || request.transitions is null)
            throw new ArgumentException("Both states and transitions arrays are required.");
        if (request.states.Count > 400 || request.transitions.Count > 1600)
            throw new ArgumentException("Workflow is limited to 400 states and 1600 transitions.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var state in request.states)
        {
            if (state is null || string.IsNullOrWhiteSpace(state.key) || string.IsNullOrWhiteSpace(state.name))
                throw new ArgumentException("Every state requires a key and name.");
            if (state.key != state.key.Trim() || !keys.Add(state.key))
                throw new ArgumentException("State keys must be unique and have no surrounding whitespace.");
        }
        var initials = request.states.Count(x => x.is_initial);
        if (initials > 1 || (publish && initials != 1))
            throw new ArgumentException("Workflow may have at most one initial state; publishing requires exactly one.");
        var transitions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var transition in request.transitions)
        {
            if (transition is null || string.IsNullOrWhiteSpace(transition.key) || string.IsNullOrWhiteSpace(transition.name))
                throw new ArgumentException("Every transition requires a key and name.");
            if (transition.key != transition.key.Trim() || !transitions.Add(transition.key))
                throw new ArgumentException("Transition keys must be unique and have no surrounding whitespace.");
            if (transition.source_key is null || transition.target_key is null ||
                !keys.Contains(transition.source_key) || !keys.Contains(transition.target_key))
                throw new ArgumentException($"Transition '{transition.key}' references an unknown state key.");
        }
    }

    private async Task ReplaceDefinitionCoreAsync(
        WorkflowVersionModel version, WorkflowGraphDefinition request, CancellationToken ct)
    {
        // A replacement must include hidden rows too, but every mutation still requires per-row authorization.
        var states = _definitionDb.Query<WorkflowStateModel>().Where(x => x.workflow_version_id == version.id).ToArray();
        var transitions = _definitionDb.Query<WorkflowTransitionModel>().Where(x => x.workflow_version_id == version.id).ToArray();
        var stateKeys = request.states.Select(x => x.key).ToHashSet(StringComparer.Ordinal);
        var transitionKeys = request.transitions.Select(x => x.key).ToHashSet(StringComparer.Ordinal);
        var removedStates = states.Where(x => !stateKeys.Contains(x.key)).Select(x => x.id).ToArray();
        var removedTransitions = transitions.Where(x => !transitionKeys.Contains(x.key)).Select(x => x.id).ToArray();
        if (_definitionDb.Query<RequestModel>().Any(x => removedStates.Contains(x.current_state_id)) ||
            _definitionDb.Query<RequestTransitionHistoryModel>().Any(x =>
                removedStates.Contains(x.from_state_id) || removedStates.Contains(x.to_state_id) ||
                removedTransitions.Contains(x.transition_id)) ||
            _definitionDb.Query<FormNodeApplicantAccessModel>().Any(x => removedStates.Contains(x.workflow_state_id)) ||
            _definitionDb.Query<WorkflowStateAccessRuleModel>().Any(x => removedStates.Contains(x.workflow_state_id)))
            throw new InvalidOperationException("Cannot remove states or transitions referenced by requests, history or access rules. Create a new workflow version instead.");
        foreach (var state in states)
            _ = await Security.GetByIdAsync<WorkflowStateModel>(state.id,
                removedStates.Contains(state.id) ? SecurityOperations.Delete : SecurityOperations.Update, ct)
                ?? throw new UnauthorizedAccessException("Workflow state mutation denied.");
        foreach (var transition in transitions)
            _ = await Security.GetByIdAsync<WorkflowTransitionModel>(transition.id,
                removedTransitions.Contains(transition.id) ? SecurityOperations.Delete : SecurityOperations.Update, ct)
                ?? throw new UnauthorizedAccessException("Workflow transition mutation denied.");
        if (request.states.Any(x => states.All(s => s.key != x.key)) ||
            request.transitions.Any(x => transitions.All(t => t.key != x.key)))
            _ = await CreationScopeGuard.RequireEntityAsync<WorkflowVersionModel>(Security, version.id, "contained", ct);

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var stateMap = states.ToDictionary(x => x.key, StringComparer.Ordinal);
        var transitionMap = transitions.ToDictionary(x => x.key, StringComparer.Ordinal);
        foreach (var input in request.states)
        {
            var exists = stateMap.TryGetValue(input.key, out var old);
            var state = exists ? old! with {
                name = input.name, description = input.description, is_initial = input.is_initial,
                is_terminal = input.is_terminal, position = input.position, updated_at = now, updated_by = actor
            } : new WorkflowStateModel(Guid.NewGuid(), version.id, input.key, input.name, input.description,
                input.is_initial, input.is_terminal, input.position, version.rbacobject_id, now, now, actor, actor);
            stateMap[input.key] = state;
            if (exists) await Security.StageUpdateAsync(state, ct);
            else await Security.StageCreateAsync(state, ct);
        }
        await Security.SaveChangesAsync(ct);

        foreach (var id in removedTransitions) await Security.StageDeleteAsync<WorkflowTransitionModel>(id, ct);
        foreach (var input in request.transitions)
        {
            var exists = transitionMap.TryGetValue(input.key, out var old);
            var transition = exists ? old! with {
                source_state_id = stateMap[input.source_key].id, target_state_id = stateMap[input.target_key].id,
                name = input.name, note_required = input.note_required,
                condition_query = input.condition_query, action_query = input.action_query,
                updated_at = now, updated_by = actor
            } : new WorkflowTransitionModel(Guid.NewGuid(), version.id, stateMap[input.source_key].id,
                stateMap[input.target_key].id, input.key, input.name, input.note_required,
                input.condition_query, input.action_query, version.rbacobject_id, now, now, actor, actor);
            if (exists) await Security.StageUpdateAsync(transition, ct);
            else await Security.StageCreateAsync(transition, ct);
        }
        await Security.SaveChangesAsync(ct);
        foreach (var id in removedStates) await Security.StageDeleteAsync<WorkflowStateModel>(id, ct);
        await Security.StageUpdateAsync(version with { updated_at = now, updated_by = actor }, ct);
        await Security.SaveChangesAsync(ct);
    }
}
