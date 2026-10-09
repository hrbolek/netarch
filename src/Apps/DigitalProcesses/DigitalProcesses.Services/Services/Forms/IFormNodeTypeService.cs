using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormNodeTypeService : IBaseService<FormNodeTypeModel, FormNodeTypeResponse>
{
    Task<FormNodeTypeResponse> CreateTypedAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default);
    Task<FormNodeTypeResponse> CreateContainedAsync(CreateFormNodeTypeRequest request, CancellationToken ct = default);
}
