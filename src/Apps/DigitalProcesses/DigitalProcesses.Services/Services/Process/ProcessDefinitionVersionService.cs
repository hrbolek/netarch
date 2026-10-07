using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IProcessDefinitionVersionService : IBaseService<ProcessDefinitionVersionModel, ProcessDefinitionVersionResponse> { Task<ProcessDefinitionVersionResponse> CreateContainedAsync(CreateProcessDefinitionVersionRequest request,CancellationToken ct=default); Task<ProcessDefinitionVersionResponse> PublishAsync(Guid id,CancellationToken ct=default); }
public sealed class ProcessDefinitionVersionService(ISecurityLayer security) : BaseService<ProcessDefinitionVersionModel,ProcessDefinitionVersionResponse>(security),IProcessDefinitionVersionService
{
    public async Task<ProcessDefinitionVersionResponse> CreateContainedAsync(CreateProcessDefinitionVersionRequest request, CancellationToken ct = default)
    {
        var (process, _) = await CreationScopeGuard.RequireEntityAsync<ProcessDefinitionModel>(
            Security, request.process_definition_id, "contained", ct);
        _ = await Security.GetByIdAsync<FormVersionModel>(request.form_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Form version not found.");
        _ = await Security.GetByIdAsync<WorkflowVersionModel>(request.workflow_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Workflow version not found.");

        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var model = new ProcessDefinitionVersionModel(
            Guid.NewGuid(), process.id, request.form_version_id, request.workflow_version_id,
            request.version, false, null, process.rbacobject_id,
            now, now, actor, actor);
        var created = await Security.ExecuteInTransactionAsync(async token =>
        {
            var x = await Security.StageCreateAsync(model, token);
            await Security.SaveChangesAsync(token);
            return ProcessDefinitionVersionResponse.FromModel(x);
        }, ct);
        return request.publish ? await PublishAsync(created.id, ct) : created;
    }

    public async Task<ProcessDefinitionVersionResponse> PublishAsync(Guid id,CancellationToken ct=default){var model=await Security.GetByIdAsync<ProcessDefinitionVersionModel>(id,SecurityOperations.Update,ct)??throw new KeyNotFoundException();if(model.is_published)return ProcessDefinitionVersionResponse.FromModel(model);var form=await Security.GetByIdAsync<FormVersionModel>(model.form_version_id,SecurityOperations.Read,ct)??throw new KeyNotFoundException();var workflow=await Security.GetByIdAsync<WorkflowVersionModel>(model.workflow_version_id,SecurityOperations.Read,ct)??throw new KeyNotFoundException();if(!form.is_published||!workflow.is_published)throw new InvalidOperationException("Process version requires published form and workflow versions.");
        var stateIds=Security.Query<WorkflowStateModel>(SecurityOperations.Read).Where(x=>x.workflow_version_id==workflow.id).Select(x=>x.id).ToArray();
        var targetNodeIds=Security.Query<WorkflowStateAccessRuleModel>(SecurityOperations.Read).Where(x=>stateIds.Contains(x.workflow_state_id)&&x.target_scope==WorkflowAccessTargetScopes.FormNode&&x.target_form_node_id!=null).Select(x=>x.target_form_node_id!.Value).Distinct().ToArray();
        if(targetNodeIds.Length>0 && Security.Query<FormNodeModel>(SecurityOperations.Read).Any(x=>targetNodeIds.Contains(x.id)&&x.form_version_id!=form.id))
            throw new InvalidOperationException("Workflow state access rules reference FormNodes outside the process FormVersion.");
        var now=DateTimeOffset.UtcNow;var updated=model with{is_published=true,published_at=now,updated_at=now,updated_by=Security.CurrentUserId};return await Security.ExecuteInTransactionAsync(async t=>{var x=await Security.StageUpdateAsync(updated,t);await Security.SaveChangesAsync(t);return ProcessDefinitionVersionResponse.FromModel(x);},ct);}
}
