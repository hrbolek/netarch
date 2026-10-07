using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowStateService : IBaseService<WorkflowStateModel, WorkflowStateResponse>
{
    Task<WorkflowStateResponse> CreateContainedAsync(CreateWorkflowStateRequest request, CancellationToken ct = default);
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
}
