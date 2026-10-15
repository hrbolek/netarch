using WebAppArchitecture.DigitalProcesses.Models.WorkflowStateAccessRule;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.WorkflowStateAccessRule;
/// <summary>
/// Služba I Workflow State Access Rule Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IWorkflowStateAccessRuleService : IBaseService<WorkflowStateAccessRuleModel>
{
    Task<WorkflowStateAccessRuleResponse> CreateContainedAsync(CreateWorkflowStateAccessRuleRequest request, CancellationToken ct = default);
}
