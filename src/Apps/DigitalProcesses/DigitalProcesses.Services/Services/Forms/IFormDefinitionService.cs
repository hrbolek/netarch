using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba I Form Definition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IFormDefinitionService : IBaseService<FormDefinitionModel>
{
    Task<FormDefinitionResponse> CreateTypedAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
    Task<FormDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default);
    Task<bool> CanCreateAsync(CancellationToken ct = default);
}
