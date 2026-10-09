using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowStateAccessRuleService : IBaseService<WorkflowStateAccessRuleModel, WorkflowStateAccessRuleResponse>
{
    Task<WorkflowStateAccessRuleResponse> CreateContainedAsync(CreateWorkflowStateAccessRuleRequest request, CancellationToken ct = default);
}
