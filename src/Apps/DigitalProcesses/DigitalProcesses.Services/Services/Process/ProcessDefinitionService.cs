using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IProcessDefinitionService : IBaseService<ProcessDefinitionModel, ProcessDefinitionResponse>
{
    Task<ProcessDefinitionResponse> CreateAsync(CreateProcessDefinitionRequest request, CancellationToken ct = default);
}

public sealed class ProcessDefinitionService(ISecurityLayer security)
    : BaseService<ProcessDefinitionModel, ProcessDefinitionResponse>(security), IProcessDefinitionService
{
    public async Task<ProcessDefinitionResponse> CreateAsync(CreateProcessDefinitionRequest r, CancellationToken ct = default)
    {
        var rbac = r.rbacobject_id ?? throw new InvalidOperationException(
            "Digital process requires an explicit RBAC Group as its security root.");
        var group = Security.Query<GroupModel>(SecurityOperations.Create)
            .SingleOrDefault(x => x.id == rbac)
            ?? throw new InvalidOperationException(
                "rbacobject_id must reference a Group on which the caller has create permission.");
        _ = await Security.GetByIdAsync<RoleTypeModel>(r.initiator_role_type_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Initiator role type not found.");

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new ProcessDefinitionModel(
            Guid.NewGuid(), r.name, r.description, r.initiator_role_type_id,
            group.id, now, now, actor, actor);

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            var created = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            return ProcessDefinitionResponse.FromModel(created);
        }, ct);
    }
}
