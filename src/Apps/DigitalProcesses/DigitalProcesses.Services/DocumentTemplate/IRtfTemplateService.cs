using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplateRevision;
using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplateRevision;

namespace WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;

public interface IRtfTemplateService
{
    Task<IReadOnlyList<RtfTemplateInfo>?> ListAsync(Guid formVersionId, CancellationToken ct = default);
    Task<RtfTemplateMutationResult> UploadAsync(Guid formVersionId, string? contentType, Stream content, string? name, CancellationToken ct = default);
    Task<RtfTemplateMutationResult> GenerateAsync(Guid formVersionId, CancellationToken ct = default);
    Task<RtfTemplateDownload?> DownloadAsync(Guid templateId, CancellationToken ct = default);
    Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken ct = default);
    Task<RtfRenderResult> RenderAsync(Guid templateId, Guid submissionId, CancellationToken ct = default);

    Task<IReadOnlyList<RtfTemplateRevisionInfo>?> ListRevisionsAsync(Guid templateId, CancellationToken ct = default);
    Task<RtfRevisionDownload?> DownloadRevisionAsync(Guid revisionId, CancellationToken ct = default);
    Task<IReadOnlyList<RtfFormSchemaNode>?> GetFormSchemaAsync(Guid formVersionId, CancellationToken ct = default);
    Task<RtfRevisionMutationResult> CreateRevisionAsync(Guid templateId, NewRtfRevisionRequest request, CancellationToken ct = default);
}
