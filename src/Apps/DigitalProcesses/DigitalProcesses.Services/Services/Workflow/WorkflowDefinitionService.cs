using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowDefinitionService : IBaseService<WorkflowDefinitionModel, WorkflowDefinitionResponse> { Task<WorkflowDefinitionResponse> CreateAsync(CreateWorkflowDefinitionRequest request,CancellationToken ct=default); }
public sealed class WorkflowDefinitionService(ISecurityLayer security) : BaseService<WorkflowDefinitionModel,WorkflowDefinitionResponse>(security),IWorkflowDefinitionService
{
    public async Task<WorkflowDefinitionResponse> CreateAsync(CreateWorkflowDefinitionRequest r,CancellationToken ct=default){var rbac=r.rbacobject_id??RbacWellKnown.RootSecurityObjectId;if(!await Security.IsAllowedAsync(rbac,SecurityOperations.Create,ct))throw new UnauthorizedAccessException();var now=DateTimeOffset.UtcNow;var a=Security.CurrentUserId;var m=new WorkflowDefinitionModel(Guid.NewGuid(),r.name,r.description,rbac,now,now,a,a);return await Security.ExecuteInTransactionAsync(async t=>{var x=await Security.StageCreateAsync(m,t);await Security.SaveChangesAsync(t);return WorkflowDefinitionResponse.FromModel(x);},ct);}
}
