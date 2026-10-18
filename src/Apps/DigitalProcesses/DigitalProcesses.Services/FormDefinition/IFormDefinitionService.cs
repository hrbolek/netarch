using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormDefinition;
/// <summary>
/// Služba I Form Definition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IFormDefinitionService : ISecuredEntityService<FormDefinitionModel>
{
    Task<FormDefinitionResponse> CreateTypedAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
    Task<FormDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default);
    Task<bool> CanCreateAsync(CancellationToken ct = default);
}
