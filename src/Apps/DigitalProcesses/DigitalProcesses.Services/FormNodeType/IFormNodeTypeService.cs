using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNodeType;
/// <summary>
/// Služba I Form Node Type Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IFormNodeTypeService : ISecuredEntityService<FormNodeTypeModel>
{
    IReadOnlyList<FormNodeTypeResponse> GetRendererTypes();
    Task<FormNodeTypeResponse> CreateTypedAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default);
    Task<FormNodeTypeResponse> CreateContainedAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default);
}
