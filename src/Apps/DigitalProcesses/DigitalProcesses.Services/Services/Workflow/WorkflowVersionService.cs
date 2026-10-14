using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba Workflow Version Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed partial class WorkflowVersionService(ISecurityLayer security, IDBLayer unsecuredDb)
    : SecuredBaseService<WorkflowVersionModel>(security, unsecuredDb), IWorkflowVersionService
{
    private readonly IDBLayer _definitionDb = unsecuredDb;

    public async Task<WorkflowVersionResponse> CreateNextAsync(Guid definitionId, CancellationToken ct = default)
    {
        _ = await Security.GetByIdAsync<WorkflowDefinitionModel>(definitionId, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Workflow definition not found or not readable.");
        var next = Security.Query<WorkflowVersionModel>(SecurityOperations.Read)
            .Where(v => v.workflow_definition_id == definitionId).Select(v => v.version).ToArray().DefaultIfEmpty(0).Max() + 1;
        return await CreateContainedAsync(new CreateWorkflowVersionRequest(definitionId, next), ct);
    }

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CurrentUserId, ExecuteInTransactionAsync, StageCreateAsync, Query; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<WorkflowVersionResponse> CreateContainedAsync(CreateWorkflowVersionRequest request, CancellationToken ct = default)
    {
        if ((request.states is null) != (request.transitions is null))
            throw new ArgumentException("Supply both states and transitions, or omit both to clone the previous version.");
        var graph = request.states is not null && request.transitions is not null
            ? new WorkflowGraphDefinition(request.states, request.transitions) : null;
        if (graph is not null) ValidateDefinition(graph, request.publish);
        var (definition, _) = await CreationScopeGuard.RequireEntityAsync<WorkflowDefinitionModel>(
            Security, request.workflow_definition_id, "contained", ct);
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new WorkflowVersionModel(
            Guid.NewGuid(), definition.id, request.version, false, null,
            definition.rbacobject_id, now, now, actor, actor);

        var created = await Security.ExecuteInTransactionAsync(async token =>
        {
            var x = await Security.StageCreateAsync(model, token);
            var source = Security.Query<WorkflowVersionModel>(SecurityOperations.Read)
                .Where(v => v.workflow_definition_id == definition.id && v.version < request.version)
                .OrderByDescending(v => v.version).FirstOrDefault();
            if (graph is not null)
            {
                await Security.SaveChangesAsync(token);
                await ReplaceDefinitionCoreAsync(model, graph, token);
            }
            else if (source is not null)
            {
                var states = Security.Query<WorkflowStateModel>(SecurityOperations.Read)
                    .Where(st => st.workflow_version_id == source.id).ToArray();
                var stateIds = states.ToDictionary(st => st.id, _ => Guid.NewGuid());
                foreach (var state in states)
                    await Security.StageCreateAsync(state with {
                        id = stateIds[state.id], workflow_version_id = model.id,
                        rbacobject_id = definition.rbacobject_id,
                        created_at = now, updated_at = now, created_by = actor, updated_by = actor
                    }, token);
                var transitions = Security.Query<WorkflowTransitionModel>(SecurityOperations.Read)
                    .Where(t => t.workflow_version_id == source.id).ToArray();
                foreach (var transition in transitions)
                    await Security.StageCreateAsync(transition with {
                        id = Guid.NewGuid(), workflow_version_id = model.id,
                        source_state_id = stateIds[transition.source_state_id],
                        target_state_id = stateIds[transition.target_state_id],
                        rbacobject_id = definition.rbacobject_id,
                        created_at = now, updated_at = now, created_by = actor, updated_by = actor
                    }, token);
                var oldStateIds = states.Select(st => st.id).ToArray();
                var rules = Security.Query<WorkflowStateAccessRuleModel>(SecurityOperations.Read)
                    .Where(r => oldStateIds.Contains(r.workflow_state_id)).ToArray();
                foreach (var rule in rules)
                    await Security.StageCreateAsync(rule with {
                        id = Guid.NewGuid(), workflow_state_id = stateIds[rule.workflow_state_id],
                        rbacobject_id = definition.rbacobject_id,
                        created_at = now, updated_at = now, created_by = actor, updated_by = actor
                    }, token);
            }
            await Security.SaveChangesAsync(token);
            if (request.publish)
            {
                return await PublishAsync(x.id, token);
            }
            return WorkflowVersionResponse.FromModel(x);
        }, ct);
        return created;
    }

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: GetByIdAsync, Query; pořadí a parametry viz implementace.
    /// </summary>
    public async Task DeleteDraftAsync(Guid id, CancellationToken ct = default)
    {
        var version = await Security.GetByIdAsync<WorkflowVersionModel>(id, SecurityOperations.Delete, ct)
            ?? throw new KeyNotFoundException("Workflow version not found or not deletable.");
        if (version.is_published) throw new InvalidOperationException("Published versions cannot be deleted.");
        if (Security.Query<ProcessDefinitionVersionModel>(SecurityOperations.Read).Any(p => p.workflow_version_id == id))
            throw new InvalidOperationException("Workflow version is referenced by a process.");
        var states = Security.Query<WorkflowStateModel>(SecurityOperations.Read).Where(st => st.workflow_version_id == id).ToArray();
        var stateIds = states.Select(st => st.id).ToArray();
        if (Security.Query<RequestModel>(SecurityOperations.Read).Any(r => stateIds.Contains(r.current_state_id)) ||
            Security.Query<RequestTransitionHistoryModel>(SecurityOperations.Read)
                .Any(h => stateIds.Contains(h.from_state_id) || stateIds.Contains(h.to_state_id)))
            throw new InvalidOperationException("Workflow state is referenced by a request or its history.");
        var transitions = Security.Query<WorkflowTransitionModel>(SecurityOperations.Read).Where(t => t.workflow_version_id == id).ToArray();
        var transitionIds = transitions.Select(t => t.id).ToArray();
        if (Security.Query<RequestTransitionHistoryModel>(SecurityOperations.Read).Any(h => transitionIds.Contains(h.transition_id)))
            throw new InvalidOperationException("Workflow transition is referenced by request history.");
        var rules = Security.Query<WorkflowStateAccessRuleModel>(SecurityOperations.Read)
            .Where(r => stateIds.Contains(r.workflow_state_id)).ToArray();
        await Security.ExecuteInTransactionAsync(async token => {
            foreach (var rule in rules) await Security.StageDeleteAsync<WorkflowStateAccessRuleModel>(rule.id, token);
            foreach (var transition in transitions) await Security.StageDeleteAsync<WorkflowTransitionModel>(transition.id, token);
            foreach (var state in states) await Security.StageDeleteAsync<WorkflowStateModel>(state.id, token);
            await Security.StageDeleteAsync<WorkflowVersionModel>(id, token);
            await Security.SaveChangesAsync(token);
        }, ct);
    }

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: GetByIdAsync, Query; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<WorkflowDesignerResponse> GetDesignerAsync(Guid id, CancellationToken ct = default)
    {
        var version = await Security.GetByIdAsync<WorkflowVersionModel>(id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Workflow version not found.");
        var states = Security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Where(x => x.workflow_version_id == id)
            .OrderBy(x => x.position).ThenBy(x => x.key)
            .Select(WorkflowStateResponse.Projection).ToArray();
        var transitions = Security.Query<WorkflowTransitionModel>(SecurityOperations.Read)
            .Where(x => x.workflow_version_id == id)
            .OrderBy(x => x.key)
            .Select(WorkflowTransitionResponse.Projection).ToArray();
        return new(WorkflowVersionResponse.FromModel(version), states, transitions);
    }

    /// <summary>
    /// Provádí přechod životního cyklu; služba ověřuje stavové předpoklady a oprávnění před uložením změn. Volání: GetByIdAsync, Query, CurrentUserId, ExecuteInTransactionAsync, StageUpdateAsync, SaveChangesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<WorkflowVersionResponse> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var model = await Security.GetByIdAsync<WorkflowVersionModel>(id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException();
        if (model.is_published) return WorkflowVersionResponse.FromModel(model);
        var initials = Security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Count(x => x.workflow_version_id == id && x.is_initial);
        if (initials != 1)
            throw new InvalidOperationException("Workflow version must have exactly one initial state before publishing.");
        var now = DateTimeOffset.UtcNow;
        var updated = model with { is_published = true, published_at = now, updated_at = now, updated_by = Security.CurrentUserId };
        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var x = await Security.StageUpdateAsync(updated, token);
            await Security.SaveChangesAsync(token);
            return WorkflowVersionResponse.FromModel(x);
        }, ct);
    }
}
