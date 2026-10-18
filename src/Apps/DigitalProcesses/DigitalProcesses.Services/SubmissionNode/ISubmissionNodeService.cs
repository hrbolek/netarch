using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using System.Text.Json;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.SubmissionNode;
/// <summary>
/// Služba I Submission Node Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface ISubmissionNodeService : ISecuredEntityService<SubmissionNodeModel>
{
    Task<SubmissionNodeResponse> CreateInSubmissionAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default);
    Task<SubmissionNodeResponse> CreateInNodeAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default);
    Task<SubmissionNodeResponse> SetValueAsync(Guid id, SetSubmissionNodeValueRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<SubmissionNodeResponse>> GetSubtreeAsync(Guid id, CancellationToken ct = default);
}
