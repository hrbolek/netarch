using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba I Form Node Type Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface IFormNodeTypeService : IBaseService<FormNodeTypeModel>
{
    Task<FormNodeTypeResponse> CreateTypedAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default);
    Task<FormNodeTypeResponse> CreateContainedAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default);
}
