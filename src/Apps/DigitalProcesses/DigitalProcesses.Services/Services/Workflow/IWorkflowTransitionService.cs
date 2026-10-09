using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowTransitionService : IBaseService<WorkflowTransitionModel, WorkflowTransitionResponse>
{
    Task<WorkflowTransitionResponse> CreateContainedAsync(CreateWorkflowTransitionRequest request, CancellationToken ct = default);
    Task UpdateDefinitionAsync(Guid id, Guid sourceId, Guid targetId, string key, string name, bool noteRequired, string? condition, string? action, CancellationToken ct = default);
    Task DeleteDefinitionAsync(Guid id, CancellationToken ct = default);
}
