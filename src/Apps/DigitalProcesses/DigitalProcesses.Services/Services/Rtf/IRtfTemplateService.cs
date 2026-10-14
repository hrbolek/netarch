using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed record RtfFormSchemaNode(Guid id, string key, string? label, string? description, Guid node_type_id);
public sealed record RtfTemplateDownload(RtfTemplateInfo Template, byte[] Content);
public sealed record RtfRevisionDownload(Guid TemplateId, int Revision, string ContentType, byte[] Content);
public sealed record RtfTemplateMutationResult(bool Found, RtfTemplateInfo? Template, int? ErrorStatus = null, string? Error = null);
public sealed record RtfRevisionMutationResult(bool Found, RtfTemplateRevisionInfo? Revision, string? Error = null);
public sealed record RtfRenderResult(bool Found, byte[]? Content = null, string? Error = null);

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
