using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormVersionService : IBaseService<FormVersionModel, FormVersionResponse>
{
    Task<FormVersionResponse> CreateContainedAsync(CreateFormVersionRequest request, CancellationToken ct = default);
    Task<FormVersionResponse> PublishAsync(Guid id, CancellationToken ct = default);
    Task DeleteDraftAsync(Guid id, CancellationToken ct = default);
}
