using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IWorkflowVersionService : IBaseService<WorkflowVersionModel, WorkflowVersionResponse>
{
    Task<WorkflowVersionResponse> CreateContainedAsync(CreateWorkflowVersionRequest request, CancellationToken ct = default);
    Task<WorkflowVersionResponse> PublishAsync(Guid id, CancellationToken ct = default);
    Task DeleteDraftAsync(Guid id, CancellationToken ct = default);
    Task<WorkflowDesignerResponse> GetDesignerAsync(Guid id, CancellationToken ct = default);
}
