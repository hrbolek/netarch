using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowVersionService : IBaseService<WorkflowVersionModel, WorkflowVersionResponse> { Task<WorkflowVersionResponse> CreateAsync(CreateWorkflowVersionRequest request,CancellationToken ct=default); Task<WorkflowVersionResponse> PublishAsync(Guid id,CancellationToken ct=default); }
public sealed class WorkflowVersionService(ISecurityLayer security) : BaseService<WorkflowVersionModel,WorkflowVersionResponse>(security),IWorkflowVersionService
{
    public async Task<WorkflowVersionResponse> CreateAsync(CreateWorkflowVersionRequest r,CancellationToken ct=default){_=await Security.GetByIdAsync<WorkflowDefinitionModel>(r.workflow_definition_id,SecurityOperations.Read,ct)??throw new KeyNotFoundException();var rbac=r.rbacobject_id??RbacWellKnown.RootSecurityObjectId;if(!await Security.IsAllowedAsync(rbac,SecurityOperations.Create,ct))throw new UnauthorizedAccessException();var now=DateTimeOffset.UtcNow;var a=Security.CurrentUserId;var m=new WorkflowVersionModel(Guid.NewGuid(),r.workflow_definition_id,r.version,false,null,rbac,now,now,a,a);var created=await Security.ExecuteInTransactionAsync(async t=>{var x=await Security.StageCreateAsync(m,t);await Security.SaveChangesAsync(t);return WorkflowVersionResponse.FromModel(x);},ct);if(r.publish)return await PublishAsync(created.id,ct);return created;}
    public async Task<WorkflowVersionResponse> PublishAsync(Guid id,CancellationToken ct=default){var model=await Security.GetByIdAsync<WorkflowVersionModel>(id,SecurityOperations.Update,ct)??throw new KeyNotFoundException();if(model.is_published)return WorkflowVersionResponse.FromModel(model);var initials=Security.Query<WorkflowStateModel>(SecurityOperations.Read).Count(x=>x.workflow_version_id==id&&x.is_initial);if(initials!=1)throw new InvalidOperationException("Workflow version must have exactly one initial state before publishing.");var now=DateTimeOffset.UtcNow;var updated=model with{is_published=true,published_at=now,updated_at=now,updated_by=Security.CurrentUserId};return await Security.ExecuteInTransactionAsync(async t=>{var x=await Security.StageUpdateAsync(updated,t);await Security.SaveChangesAsync(t);return WorkflowVersionResponse.FromModel(x);},ct);}
}
