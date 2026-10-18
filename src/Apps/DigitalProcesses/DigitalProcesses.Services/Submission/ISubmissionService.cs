using WebAppArchitecture.DigitalProcesses.Models.Submission;
using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.Submission;
/// <summary>
/// Služba I Submission Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface ISubmissionService : ISecuredEntityService<SubmissionModel>, IFormDocumentService
{
    Task<SubmissionTreeResponse> GetTreeAsync(Guid submissionId, CancellationToken ct = default);
    Task<SubmissionTreeResponse> UpdateValuesAsync(Guid id, BulkSubmissionValuesRequest request, CancellationToken ct = default);
}
