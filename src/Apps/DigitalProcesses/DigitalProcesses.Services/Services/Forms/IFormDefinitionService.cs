using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormDefinitionService : IBaseService<FormDefinitionModel, FormDefinitionResponse>
{
    Task<FormDefinitionResponse> CreateTypedAsync(CreateFormDefinitionRequest request, CancellationToken ct = default);
    Task<FormDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default);
}
