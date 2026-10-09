using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowStateService : IBaseService<WorkflowStateModel, WorkflowStateResponse>
{
    Task<WorkflowStateResponse> CreateContainedAsync(CreateWorkflowStateRequest request, CancellationToken ct = default);
    Task UpdateDefinitionAsync(Guid id, string key, string name, string? description, bool isInitial, bool isTerminal, int position, CancellationToken ct = default);
    Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default);
}

public sealed class WorkflowStateService(ISecurityLayer security)
    : BaseService<WorkflowStateModel, WorkflowStateResponse>(security), IWorkflowStateService
{
    public async Task<WorkflowStateResponse> CreateContainedAsync(CreateWorkflowStateRequest request, CancellationToken ct = default)
    {
        var (workflowVersion, _) = await CreationScopeGuard.RequireEntityAsync<WorkflowVersionModel>(
            Security, request.workflow_version_id, "contained", ct);
        if (workflowVersion.is_published)
            throw new InvalidOperationException("Published workflow version is immutable.");
        if (request.is_initial && Security.Query<WorkflowStateModel>(SecurityOperations.Read)
                .Any(x => x.workflow_version_id == request.workflow_version_id && x.is_initial))
            throw new InvalidOperationException("Workflow version already has an initial state.");

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new WorkflowStateModel(
            Guid.NewGuid(), workflowVersion.id, request.key, request.name, request.description,
            request.is_initial, request.is_terminal, request.position,
            workflowVersion.rbacobject_id, now, now, actor, actor);
        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var x = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            return WorkflowStateResponse.FromModel(x);
        }, ct);
    }
    public async Task UpdateDefinitionAsync(Guid id, string key, string name, string? description, bool isInitial, bool isTerminal, int position, CancellationToken ct = default)
    {
        var state = await Security.GetByIdAsync<WorkflowStateModel>(id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("State not editable.");
        var version = await Security.GetByIdAsync<WorkflowVersionModel>(state.workflow_version_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Version not editable.");
        if (version.is_published) throw new InvalidOperationException("Published workflow is immutable.");
        if (isInitial && Security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Any(x => x.workflow_version_id == version.id && x.id != id && x.is_initial))
            throw new InvalidOperationException("Only one state may be initial.");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Key and name are required.");
        await Security.StageUpdateAsync(state with { key = key, name = name, description = description,
            is_initial = isInitial, is_terminal = isTerminal, position = position,
            updated_at = DateTimeOffset.UtcNow, updated_by = Security.CurrentUserId }, ct);
        await Security.SaveChangesAsync(ct);
    }

    public async Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default)
    {
        var state = await Security.GetByIdAsync<WorkflowStateModel>(id, SecurityOperations.Delete, ct)
            ?? throw new KeyNotFoundException("State not deletable.");
        var version = await Security.GetByIdAsync<WorkflowVersionModel>(state.workflow_version_id, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Version not editable.");
        if (version.is_published) throw new InvalidOperationException("Published workflow is immutable.");
        if (Security.Query<WorkflowTransitionModel>(SecurityOperations.Read)
            .Any(x => x.source_state_id == id || x.target_state_id == id))
            throw new InvalidOperationException("Delete transitions connected to this state first.");
        await Security.StageDeleteAsync<WorkflowStateModel>(id, ct);
        await Security.SaveChangesAsync(ct);
    }

}
