using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowStateService : IBaseService<WorkflowStateModel, WorkflowStateResponse> { Task<WorkflowStateResponse> CreateAsync(CreateWorkflowStateRequest request,CancellationToken ct=default); }
public sealed class WorkflowStateService(ISecurityLayer security) : BaseService<WorkflowStateModel,WorkflowStateResponse>(security),IWorkflowStateService
{
    public async Task<WorkflowStateResponse> CreateAsync(CreateWorkflowStateRequest r,CancellationToken ct=default){var workflowVersion=await Security.GetByIdAsync<WorkflowVersionModel>(r.workflow_version_id,SecurityOperations.Update,ct)??throw new KeyNotFoundException();if(workflowVersion.is_published)throw new InvalidOperationException("Published workflow version is immutable.");if(r.is_initial && Security.Query<WorkflowStateModel>(SecurityOperations.Read).Any(x=>x.workflow_version_id==r.workflow_version_id&&x.is_initial))throw new InvalidOperationException("Workflow version already has an initial state.");var rbac=r.rbacobject_id??RbacWellKnown.RootSecurityObjectId;if(!await Security.IsAllowedAsync(rbac,SecurityOperations.Create,ct))throw new UnauthorizedAccessException();var now=DateTimeOffset.UtcNow;var a=Security.CurrentUserId;var m=new WorkflowStateModel(Guid.NewGuid(),r.workflow_version_id,r.key,r.name,r.description,r.is_initial,r.is_terminal,r.position,rbac,now,now,a,a);return await Security.ExecuteInTransactionAsync(async t=>{var x=await Security.StageCreateAsync(m,t);await Security.SaveChangesAsync(t);return WorkflowStateResponse.FromModel(x);},ct);}
}
