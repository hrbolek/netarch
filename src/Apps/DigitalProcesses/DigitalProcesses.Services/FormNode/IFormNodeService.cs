using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNode;
/// <summary>
/// Služba I Form Node Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IFormNodeService : IBaseService<FormNodeModel>, IFormNodeInsertionService
{
    Task<FormNodeResponse> CreateContainedInVersionAsync(CreateFormNodeRequest request, CancellationToken ct = default);
    Task<FormNodeResponse> CreateContainedInNodeAsync(CreateFormNodeRequest request, CancellationToken ct = default);
    Task<FormNodeResponse> MoveAsync(Guid id, MoveFormNodeRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<FormNodeResponse>> GetSubtreeAsync(Guid id, CancellationToken ct = default);
    Task<FormTreeResponse> GetTreeAsync(Guid formVersionId, CancellationToken ct = default);
    Task<FormDesignerResponse> GetDesignerAsync(Guid formVersionId, CancellationToken ct = default);
    Task<FormDesignerPageResponse> GetEditorAsync(Guid nodeId, bool testMode = false, CancellationToken ct = default);
    Task<FormNodeResponse> UpdateDefinitionAsync(Guid id, UpdateFormNodeRequest request, CancellationToken ct = default);
    Task DeleteSubtreeAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyCollection<FormNodeResponse>> ImportSubtreeAsync(ImportFormSubtreeRequest request, CancellationToken ct = default);
}
