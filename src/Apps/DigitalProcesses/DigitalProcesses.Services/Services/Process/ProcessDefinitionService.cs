using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IProcessDefinitionService : IBaseService<ProcessDefinitionModel, ProcessDefinitionResponse>
{
    Task<ProcessDefinitionResponse> CreateTypedAsync(CreateProcessDefinitionRequest request, CancellationToken ct = default);
}

public sealed class ProcessDefinitionService(ISecurityLayer security, ISecurityProjectionService securityProjection)
    : BaseService<ProcessDefinitionModel, ProcessDefinitionResponse>(security), IProcessDefinitionService
{
    public async Task<ProcessDefinitionResponse> CreateTypedAsync(CreateProcessDefinitionRequest request, CancellationToken ct = default)
    {
        _ = await Security.GetByIdAsync<RoleTypeModel>(request.initiator_role_type_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Initiator role type not found.");
        var (catalog, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, RbacWellKnown.ProcessDefinitionCreationScopeId, "typed", ct);

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        return await Security.ExecuteInTransactionAsync(async token =>
        {
            await CreationScopeGuard.StageOwnedGroupAsync(
                Security, id, $"Process definition: {request.name}", catalog, true, now, actor, token);
            var model = new ProcessDefinitionModel(
                id, request.name, request.description, request.initiator_role_type_id,
                id, now, now, actor, actor);
            var created = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            await securityProjection.RebuildAllAsync(token);
            return ProcessDefinitionResponse.FromModel(created);
        }, ct);
    }
}
