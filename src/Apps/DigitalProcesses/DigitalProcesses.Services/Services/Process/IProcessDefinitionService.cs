using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba I Process Definition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IProcessDefinitionService : IBaseService<ProcessDefinitionModel>
{
    Task<ProcessDefinitionResponse> CreateTypedAsync(CreateProcessDefinitionRequest request, CancellationToken ct = default);
    Task<ProcessDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default);
}
