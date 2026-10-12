using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba I Workflow Transition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IWorkflowTransitionService : IBaseService<WorkflowTransitionModel>
{
    Task<WorkflowTransitionResponse> CreateContainedAsync(CreateWorkflowTransitionRequest request, CancellationToken ct = default);
    Task UpdateDefinitionAsync(Guid id, Guid sourceId, Guid targetId, string key, string name, bool noteRequired, string? condition, string? action, CancellationToken ct = default);
    Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default);
}
