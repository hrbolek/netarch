using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Text.Json;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba I Submission Node Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public interface ISubmissionNodeService : IBaseService<SubmissionNodeModel>
{
    Task<SubmissionNodeResponse> CreateInSubmissionAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default);
    Task<SubmissionNodeResponse> CreateInNodeAsync(CreateSubmissionNodeRequest request, CancellationToken ct = default);
    Task<SubmissionNodeResponse> SetValueAsync(Guid id, SetSubmissionNodeValueRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<SubmissionNodeResponse>> GetSubtreeAsync(Guid id, CancellationToken ct = default);
}
