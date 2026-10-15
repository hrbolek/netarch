using WebAppArchitecture.DigitalProcesses.Models.Shared;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.Submission;
public interface IFormDocumentService
{
    Task<AuxiliaryOperationResult> SchemaAsync(Guid id, CancellationToken ct);
    Task<AuxiliaryOperationResult> DataAsync(Guid id, CancellationToken ct);
    Task<AuxiliaryOperationResult> DataListAsync(Guid id, CancellationToken ct);
    JsonObject Reconstruct(Guid submissionId, Guid formVersionId);
}
