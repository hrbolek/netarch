using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplateRevision;
using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplateRevision;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;

namespace WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;

public sealed class RtfTemplateService(IFormVersionService versions) : IRtfTemplateService
{
    public Task<IReadOnlyList<RtfTemplateInfo>?> ListAsync(Guid formVersionId, CancellationToken ct = default) => versions.ListAsync(formVersionId, ct);
    public Task<RtfTemplateMutationResult> UploadAsync(Guid formVersionId, string? contentType, Stream content, string? name, CancellationToken ct = default) => versions.UploadAsync(formVersionId, contentType, content, name, ct);
    public Task<RtfTemplateMutationResult> GenerateAsync(Guid formVersionId, CancellationToken ct = default) => versions.GenerateAsync(formVersionId, ct);
    public Task<RtfTemplateDownload?> DownloadAsync(Guid templateId, CancellationToken ct = default) => versions.DownloadAsync(templateId, ct);
    public Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken ct = default) => versions.DeleteTemplateAsync(templateId, ct);
    public Task<RtfRenderResult> RenderAsync(Guid templateId, Guid submissionId, CancellationToken ct = default) => versions.RenderAsync(templateId, submissionId, ct);
    public Task<IReadOnlyList<RtfTemplateRevisionInfo>?> ListRevisionsAsync(Guid templateId, CancellationToken ct = default) => versions.ListRevisionsAsync(templateId, ct);
    public Task<RtfRevisionDownload?> DownloadRevisionAsync(Guid revisionId, CancellationToken ct = default) => versions.DownloadRevisionAsync(revisionId, ct);
    public Task<IReadOnlyList<RtfFormSchemaNode>?> GetFormSchemaAsync(Guid formVersionId, CancellationToken ct = default) => versions.GetFormSchemaAsync(formVersionId, ct);
    public Task<RtfRevisionMutationResult> CreateRevisionAsync(Guid templateId, NewRtfRevisionRequest request, CancellationToken ct = default) => versions.CreateRevisionAsync(templateId, request, ct);
}
