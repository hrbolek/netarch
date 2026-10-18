using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.WorkflowTransition;
/// <summary>
/// Služba I Workflow Transition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IWorkflowTransitionService : ISecuredEntityService<WorkflowTransitionModel>
{
    Task<WorkflowTransitionResponse> CreateContainedAsync(CreateWorkflowTransitionRequest request, CancellationToken ct = default);
    Task UpdateDefinitionAsync(Guid id, Guid sourceId, Guid targetId, string key, string name, bool noteRequired, string? condition, string? action, CancellationToken ct = default);
    Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default);
}
