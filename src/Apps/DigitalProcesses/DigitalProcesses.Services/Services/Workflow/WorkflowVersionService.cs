using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowVersionService : IBaseService<WorkflowVersionModel, WorkflowVersionResponse>
{
    Task<WorkflowVersionResponse> CreateContainedAsync(CreateWorkflowVersionRequest request, CancellationToken ct = default);
    Task<WorkflowVersionResponse> PublishAsync(Guid id, CancellationToken ct = default);
}

public sealed class WorkflowVersionService(ISecurityLayer security)
    : BaseService<WorkflowVersionModel, WorkflowVersionResponse>(security), IWorkflowVersionService
{
    public async Task<WorkflowVersionResponse> CreateContainedAsync(CreateWorkflowVersionRequest request, CancellationToken ct = default)
    {
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
            await Security.SaveChangesAsync(token);
            return WorkflowVersionResponse.FromModel(x);
        }, ct);
        return request.publish ? await PublishAsync(created.id, ct) : created;
    }

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
