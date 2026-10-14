using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba I Workflow Version Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IWorkflowVersionService : IBaseService<WorkflowVersionModel>
{
    Task<WorkflowVersionResponse> CreateContainedAsync(CreateWorkflowVersionRequest request, CancellationToken ct = default);
    Task<WorkflowVersionResponse> PublishAsync(Guid id, CancellationToken ct = default);
    Task DeleteDraftAsync(Guid id, CancellationToken ct = default);
    Task<WorkflowDesignerResponse> GetDesignerAsync(Guid id, CancellationToken ct = default);
    Task<WorkflowDesignerResponse> ReplaceDefinitionAsync(Guid id, WorkflowGraphDefinition request, CancellationToken ct = default);
    Task<WorkflowVersionResponse> CreateNextAsync(Guid definitionId, CancellationToken ct = default);
}
