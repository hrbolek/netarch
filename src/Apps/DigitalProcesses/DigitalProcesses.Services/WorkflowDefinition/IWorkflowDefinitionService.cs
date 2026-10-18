using WebAppArchitecture.DigitalProcesses.Models.WorkflowDefinition;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.WorkflowDefinition;
/// <summary>
/// Služba I Workflow Definition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IWorkflowDefinitionService : ISecuredEntityService<WorkflowDefinitionModel>
{
    Task<WorkflowDefinitionResponse> CreateTypedAsync(CreateWorkflowDefinitionRequest request, CancellationToken ct = default);
    Task<WorkflowDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default);
}
