using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.Models.Shared;

namespace WebAppArchitecture.DigitalProcesses.Services.Submission;



public sealed class FormDocumentService(ISubmissionService submissions) : IFormDocumentService
{
    public Task<AuxiliaryOperationResult> SchemaAsync(Guid id, CancellationToken ct) => submissions.SchemaAsync(id, ct);
    public Task<AuxiliaryOperationResult> DataAsync(Guid id, CancellationToken ct) => submissions.DataAsync(id, ct);
    public Task<AuxiliaryOperationResult> DataListAsync(Guid id, CancellationToken ct) => submissions.DataListAsync(id, ct);
    public JsonObject Reconstruct(Guid submissionId, Guid formVersionId) => submissions.Reconstruct(submissionId, formVersionId);
}
