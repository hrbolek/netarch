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
