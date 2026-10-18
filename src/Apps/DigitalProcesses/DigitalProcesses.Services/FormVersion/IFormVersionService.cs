using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormVersion;
/// <summary>
/// Služba I Form Version Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IFormVersionService : ISecuredEntityService<FormVersionModel>, IRtfTemplateService
{
    Task<FormTemplatesPageData?> TemplatePageAsync(Guid id, CancellationToken ct = default);
    Task<FormVersionResponse> CreateContainedAsync(CreateFormVersionRequest request, CancellationToken ct = default);
    Task<FormVersionResponse> PublishAsync(Guid id, CancellationToken ct = default);
    Task DeleteDraftAsync(Guid id, CancellationToken ct = default);
    Task<FormVersionResponse> CreateNextAsync(Guid definitionId, CancellationToken ct = default);
    Task<FormTreeResponse> UpsertTreeAsync(Guid id, BulkFormTreeRequest request, CancellationToken ct = default);
}
