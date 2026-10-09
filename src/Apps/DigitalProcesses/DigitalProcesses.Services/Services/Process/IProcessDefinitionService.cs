using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IProcessDefinitionService : IBaseService<ProcessDefinitionModel, ProcessDefinitionResponse>
{
    Task<ProcessDefinitionResponse> CreateTypedAsync(CreateProcessDefinitionRequest request, CancellationToken ct = default);
}
