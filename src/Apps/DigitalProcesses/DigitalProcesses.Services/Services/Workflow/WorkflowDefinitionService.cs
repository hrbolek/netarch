using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowDefinitionService : IBaseService<WorkflowDefinitionModel, WorkflowDefinitionResponse>
{
    Task<WorkflowDefinitionResponse> CreateTypedAsync(CreateWorkflowDefinitionRequest request, CancellationToken ct = default);
}

public sealed class WorkflowDefinitionService(ISecurityLayer security, ISecurityProjectionService securityProjection)
    : BaseService<WorkflowDefinitionModel, WorkflowDefinitionResponse>(security), IWorkflowDefinitionService
{
    public async Task<WorkflowDefinitionResponse> CreateTypedAsync(CreateWorkflowDefinitionRequest request, CancellationToken ct = default)
    {
        var (catalog, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, RbacWellKnown.WorkflowDefinitionCreationScopeId, "typed", ct);
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            await CreationScopeGuard.StageOwnedGroupAsync(
                Security, id, $"Workflow definition: {request.name}", catalog, true, now, actor, token);
            var model = new WorkflowDefinitionModel(id, request.name, request.description, id, now, now, actor, actor);
            var created = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            await securityProjection.RebuildAllAsync(token);
            return WorkflowDefinitionResponse.FromModel(created);
        }, ct);
    }
}
