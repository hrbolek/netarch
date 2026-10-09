using System.Text.Json;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface ISubmissionNodeService : IBaseService<SubmissionNodeModel, SubmissionNodeResponse>
{
    Task<SubmissionNodeResponse> CreateInSubmissionAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default);
    Task<SubmissionNodeResponse> CreateInNodeAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default);
    Task<SubmissionNodeResponse> SetValueAsync(Guid id, SetSubmissionNodeValueRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<SubmissionNodeResponse>> GetSubtreeAsync(Guid id, CancellationToken ct = default);
}
