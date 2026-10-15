using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.WorkflowState;
/// <summary>
/// Služba I Workflow State Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IWorkflowStateService : IBaseService<WorkflowStateModel>
{
    Task<WorkflowStateResponse> CreateContainedAsync(CreateWorkflowStateRequest request, CancellationToken ct = default);
    Task UpdateDefinitionAsync(Guid id, string key, string name, string? description, bool isInitial, bool isTerminal, int position, CancellationToken ct = default);
    Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default);
}
