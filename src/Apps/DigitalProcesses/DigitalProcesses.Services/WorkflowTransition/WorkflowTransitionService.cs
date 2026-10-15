using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.WorkflowTransition;
/// <summary>
/// Služba Workflow Transition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class WorkflowTransitionService(ISecurityLayer security, IDBLayer unsecuredDb)
    : SecuredBaseService<WorkflowTransitionModel>(security, unsecuredDb), IWorkflowTransitionService
{
    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: GetByIdAsync, CurrentUserId; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<WorkflowTransitionResponse> CreateContainedAsync(CreateWorkflowTransitionRequest request, CancellationToken ct = default)
    {
        var (workflowVersion, _) = await CreationScopeGuard.RequireEntityAsync<WorkflowVersionModel>(
            Security, request.workflow_version_id, "contained", ct);
        if (workflowVersion.is_published)
            throw new InvalidOperationException("Published workflow version is immutable.");

        var source = await Security.GetByIdAsync<WorkflowStateModel>(request.source_state_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Source state not found.");
        var target = await Security.GetByIdAsync<WorkflowStateModel>(request.target_state_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Target state not found.");
        if (source.workflow_version_id != workflowVersion.id || target.workflow_version_id != workflowVersion.id)
            throw new InvalidOperationException("Transition states must belong to the same workflow version.");

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new WorkflowTransitionModel(
            Guid.NewGuid(), workflowVersion.id, source.id, target.id, request.key, request.name,
            request.note_required, request.condition_query, request.action_query,
            workflowVersion.rbacobject_id, now, now, actor, actor);
        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var x = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            return WorkflowTransitionResponse.FromModel(x);
        }, ct);
    }
    /// <summary>
    /// Zpracovává změnu existující entity; ověření oprávnění a integrity musí probíhat ve službě, ne pouze v UI. Volání: GetByIdAsync, StageUpdateAsync, CurrentUserId, SaveChangesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task UpdateDefinitionAsync(Guid id, Guid sourceId, Guid targetId, string key, string name, bool noteRequired, string? condition, string? action, CancellationToken ct = default)
    {
        var transition = await Security.GetByIdAsync<WorkflowTransitionModel>(id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Transition not editable.");
        var version = await Security.GetByIdAsync<WorkflowVersionModel>(transition.workflow_version_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Version not editable.");
        if (version.is_published) throw new InvalidOperationException("Published workflow is immutable.");
        var source = await Security.GetByIdAsync<WorkflowStateModel>(sourceId, SecurityOperations.Read, ct);
        var target = await Security.GetByIdAsync<WorkflowStateModel>(targetId, SecurityOperations.Read, ct);
        if (source?.workflow_version_id != version.id || target?.workflow_version_id != version.id)
            throw new InvalidOperationException("Both states must belong to the current workflow version.");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Key and name are required.");
        await Security.StageUpdateAsync(transition with { source_state_id = sourceId, target_state_id = targetId,
            key = key, name = name, note_required = noteRequired, condition_query = condition, action_query = action,
            updated_at = DateTimeOffset.UtcNow, updated_by = Security.CurrentUserId }, ct);
        await Security.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Požaduje odstranění entity; pravidla mazání závislostí a RBAC se uplatňují ve službě. Volání: GetByIdAsync, StageDeleteAsync, SaveChangesAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default)
    {
        var transition = await Security.GetByIdAsync<WorkflowTransitionModel>(id, SecurityOperations.Delete, ct)
            ?? throw new KeyNotFoundException("Transition not deletable.");
        var version = await Security.GetByIdAsync<WorkflowVersionModel>(transition.workflow_version_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Version not editable.");
        if (version.is_published) throw new InvalidOperationException("Published workflow is immutable.");
        await Security.StageDeleteAsync<WorkflowTransitionModel>(id, ct);
        await Security.SaveChangesAsync(ct);
    }

}
