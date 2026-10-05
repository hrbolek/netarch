using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowTransitionService : IBaseService<WorkflowTransitionModel, WorkflowTransitionResponse> { Task<WorkflowTransitionResponse> CreateAsync(CreateWorkflowTransitionRequest request,CancellationToken ct=default); }
public sealed class WorkflowTransitionService(ISecurityLayer security) : BaseService<WorkflowTransitionModel,WorkflowTransitionResponse>(security),IWorkflowTransitionService
{
    public async Task<WorkflowTransitionResponse> CreateAsync(CreateWorkflowTransitionRequest r,CancellationToken ct=default){var workflowVersion=await Security.GetByIdAsync<WorkflowVersionModel>(r.workflow_version_id,SecurityOperations.Update,ct)??throw new KeyNotFoundException("Workflow version not found.");if(workflowVersion.is_published)throw new InvalidOperationException("Published workflow version is immutable.");var s=await Security.GetByIdAsync<WorkflowStateModel>(r.source_state_id,SecurityOperations.Read,ct)??throw new KeyNotFoundException("Source state not found.");var d=await Security.GetByIdAsync<WorkflowStateModel>(r.target_state_id,SecurityOperations.Read,ct)??throw new KeyNotFoundException("Target state not found.");if(s.workflow_version_id!=r.workflow_version_id||d.workflow_version_id!=r.workflow_version_id)throw new InvalidOperationException("Transition states must belong to the same workflow version.");var rbac=r.rbacobject_id??RbacWellKnown.RootSecurityObjectId;if(!await Security.IsAllowedAsync(rbac,SecurityOperations.Create,ct))throw new UnauthorizedAccessException();var now=DateTimeOffset.UtcNow;var a=Security.CurrentUserId;var m=new WorkflowTransitionModel(Guid.NewGuid(),r.workflow_version_id,r.source_state_id,r.target_state_id,r.key,r.name,r.note_required,r.condition_query,r.action_query,rbac,now,now,a,a);return await Security.ExecuteInTransactionAsync(async t=>{var x=await Security.StageCreateAsync(m,t);await Security.SaveChangesAsync(t);return WorkflowTransitionResponse.FromModel(x);},ct);}
}
