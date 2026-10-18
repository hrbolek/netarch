using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.WorkflowVersion;
/// <summary>
/// Služba I Workflow Version Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IWorkflowVersionService : ISecuredEntityService<WorkflowVersionModel>
{
    Task<WorkflowVersionResponse> CreateContainedAsync(CreateWorkflowVersionRequest request, CancellationToken ct = default);
    Task<WorkflowVersionResponse> PublishAsync(Guid id, CancellationToken ct = default);
    Task DeleteDraftAsync(Guid id, CancellationToken ct = default);
    Task<WorkflowDesignerResponse> GetDesignerAsync(Guid id, CancellationToken ct = default);
    Task<WorkflowDesignerResponse> ReplaceDefinitionAsync(Guid id, WorkflowGraphDefinition request, CancellationToken ct = default);
    Task<WorkflowVersionResponse> CreateNextAsync(Guid definitionId, CancellationToken ct = default);
}
