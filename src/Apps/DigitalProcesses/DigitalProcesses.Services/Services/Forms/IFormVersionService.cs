using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba I Form Version Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IFormVersionService : IBaseService<FormVersionModel>
{
    Task<FormVersionResponse> CreateContainedAsync(CreateFormVersionRequest request, CancellationToken ct = default);
    Task<FormVersionResponse> PublishAsync(Guid id, CancellationToken ct = default);
    Task DeleteDraftAsync(Guid id, CancellationToken ct = default);
    Task<FormVersionResponse> CreateNextAsync(Guid definitionId, CancellationToken ct = default);
    Task<FormTreeResponse> UpsertTreeAsync(Guid id, BulkFormTreeRequest request, CancellationToken ct = default);
}
