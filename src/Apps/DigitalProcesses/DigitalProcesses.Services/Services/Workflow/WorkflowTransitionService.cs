using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowTransitionService : IBaseService<WorkflowTransitionModel, WorkflowTransitionResponse>
{
    Task<WorkflowTransitionResponse> CreateContainedAsync(CreateWorkflowTransitionRequest request, CancellationToken ct = default);
}

public sealed class WorkflowTransitionService(ISecurityLayer security)
    : BaseService<WorkflowTransitionModel, WorkflowTransitionResponse>(security), IWorkflowTransitionService
{
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
}
