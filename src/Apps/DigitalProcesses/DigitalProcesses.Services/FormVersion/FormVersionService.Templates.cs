using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplateRevision;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.TemplateFieldBinding;
using WebAppArchitecture.DigitalProcesses.Services.DocumentTemplateRevision;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using System.Text;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormVersion;

public sealed partial class FormVersionService
{
    public async Task<IReadOnlyList<RtfTemplateInfo>?> ListAsync(Guid formVersionId, CancellationToken ct = default)
    {
        if (await Security.GetByIdAsync<FormVersionModel>(formVersionId, SecurityOperations.Read, ct) is null)
            return null;
        return store.List(formVersionId);
    }

    public async Task<RtfTemplateMutationResult> UploadAsync(
        Guid formVersionId, string? contentType, Stream content, string? name, CancellationToken ct = default)
    {
        if (await Security.GetByIdAsync<FormVersionModel>(formVersionId, SecurityOperations.Update, ct) is null)
            return new(false, null);
        if (!(contentType?.StartsWith("application/rtf", StringComparison.OrdinalIgnoreCase) ?? false))
            return new(true, null, 415, "Send raw RTF with Content-Type: application/rtf");

        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var count = await content.ReadAsync(buffer, ct);
            if (count == 0) break;
            if (memory.Length + count > 5_000_000)
                return new(true, null, 400, "RTF template exceeds 5 MB.");
            memory.Write(buffer, 0, count);
        }
        var bytes = memory.ToArray();
        if (!RtfTemplateEngine.IsRtf(bytes))
            return new(true, null, 400, "Payload is not an RTF document.");
        if (!RtfTemplateEngine.IsValidTemplate(bytes, out var validation))
            return new(true, null, 400, validation);

        var item = await Security.ExecuteInTransactionAsync(async token =>
        {
            var saved = await store.SaveAsync(
                formVersionId,
                string.IsNullOrWhiteSpace(name) ? "RTF template" : name,
                bytes,
                token);
            await Security.SaveChangesAsync(token);
            return saved;
        }, ct);
        return new(true, item);
    }

    public async Task<RtfTemplateMutationResult> GenerateAsync(Guid formVersionId, CancellationToken ct = default)
    {
        if (await Security.GetByIdAsync<FormVersionModel>(formVersionId, SecurityOperations.Update, ct) is null)
            return new(false, null);
        var definitions = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(node => node.form_version_id == formVersionId).ToArray();
        var typeIds = definitions.Select(node => node.node_type_id).Distinct().ToArray();
        var types = Security.Query<FormNodeTypeModel>(SecurityOperations.Read)
            .Where(type => typeIds.Contains(type.id)).ToDictionary(type => type.id);
        var bytes = Encoding.ASCII.GetBytes(RtfTemplateEngine.Generate(definitions, types));
        var item = await Security.ExecuteInTransactionAsync(async token =>
        {
            var saved = await store.SaveAsync(formVersionId, "Generated from schema", bytes, token);
            await Security.SaveChangesAsync(token);
            return saved;
        }, ct);
        return new(true, item);
    }

    public async Task<RtfTemplateDownload?> DownloadAsync(Guid templateId, CancellationToken ct = default)
    {
        var template = store.Find(templateId);
        if (template is null ||
            await Security.GetByIdAsync<FormVersionModel>(template.FormVersionId, SecurityOperations.Read, ct) is null)
            return null;
        return new(template, store.Read(templateId));
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId, CancellationToken ct = default)
    {
        var template = store.Find(templateId);
        if (template is null ||
            await Security.GetByIdAsync<FormVersionModel>(template.FormVersionId, SecurityOperations.Update, ct) is null)
            return false;
        await Security.ExecuteInTransactionAsync(async token =>
        {
            await store.StageDeleteAsync(templateId, token);
            await Security.SaveChangesAsync(token);
        }, ct);
        return true;
    }

    public async Task<RtfRenderResult> RenderAsync(Guid templateId, Guid submissionId, CancellationToken ct = default)
    {
        var template = store.Find(templateId);
        if (template is null ||
            await Security.GetByIdAsync<FormVersionModel>(template.FormVersionId, SecurityOperations.Read, ct) is null)
            return new(false);
        var submission = await Security.GetByIdAsync<SubmissionModel>(submissionId, SecurityOperations.Read, ct);
        if (submission is null || submission.form_version_id != template.FormVersionId)
            return new(false);

        var data = formDocuments.Reconstruct(submission.id, submission.form_version_id);
        try
        {
            return new(true, RtfTemplateEngine.Render(store.Read(templateId), data, submissionId));
        }
        catch (FormatException ex)
        {
            return new(true, null, ex.Message);
        }
    }

    public async Task<IReadOnlyList<RtfTemplateRevisionInfo>?> ListRevisionsAsync(Guid templateId, CancellationToken ct = default)
    {
        var template = store.Find(templateId);
        if (template is null ||
            await Security.GetByIdAsync<FormVersionModel>(template.FormVersionId, SecurityOperations.Read, ct) is null)
            return null;
        return store.ListRevisions(templateId);
    }

    public async Task<RtfRevisionDownload?> DownloadRevisionAsync(Guid revisionId, CancellationToken ct = default)
    {
        var revision = store.FindRevision(revisionId);
        if (revision is null) return null;
        var template = store.Find(revision.template_id);
        if (template is null ||
            await Security.GetByIdAsync<FormVersionModel>(template.FormVersionId, SecurityOperations.Read, ct) is null)
            return null;
        return new(template.Id, revision.revision, revision.content_type, revision.content);
    }

    public async Task<IReadOnlyList<RtfFormSchemaNode>?> GetFormSchemaAsync(Guid formVersionId, CancellationToken ct = default)
    {
        if (await Security.GetByIdAsync<FormVersionModel>(formVersionId, SecurityOperations.Read, ct) is null)
            return null;
        return Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(node => node.form_version_id == formVersionId)
            .Select(node => new RtfFormSchemaNode(node.id, node.key, node.label, node.description, node.node_type_id))
            .ToArray();
    }

    public async Task<RtfRevisionMutationResult> CreateRevisionAsync(
        Guid templateId, NewRtfRevisionRequest request, CancellationToken ct = default)
    {
        var template = store.Find(templateId);
        if (template is null ||
            await Security.GetByIdAsync<FormVersionModel>(template.FormVersionId, SecurityOperations.Update, ct) is null)
            return new(false, null);

        byte[] bytes;
        try { bytes = Convert.FromBase64String(request.RtfBase64); }
        catch (FormatException) { return new(true, null, "Invalid Base64."); }
        if (bytes.Length == 0 || bytes.Length > 5_000_000 ||
            !RtfTemplateEngine.IsRtf(bytes) || !RtfTemplateEngine.IsValidTemplate(bytes, out _))
            return new(true, null, "Invalid RTF or size exceeds 5 MB.");

        var mappings = request.Bindings ?? Array.Empty<RtfFieldBindingRequest>();
        if (mappings.Any(binding => string.IsNullOrWhiteSpace(binding.Placeholder)) ||
            mappings.Select(binding => binding.Placeholder).Distinct(StringComparer.Ordinal).Count() != mappings.Length)
            return new(true, null, "Placeholders must be nonempty and unique.");
        var nodeIds = mappings.Select(binding => binding.FormNodeId).Distinct().ToArray();
        var readableIds = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(node => node.form_version_id == template.FormVersionId && nodeIds.Contains(node.id))
            .Select(node => node.id).ToArray();
        if (readableIds.Length != nodeIds.Length)
            return new(true, null, "A binding references an absent or unreadable FormNode.");

        var created = await Security.ExecuteInTransactionAsync(async token =>
        {
            var revision = new DocumentTemplateRevisionModel
            {
                id = Guid.NewGuid(),
                template_id = templateId,
                revision = store.NextRevision(templateId),
                content = bytes,
                content_type = "application/rtf",
                created_at = DateTimeOffset.UtcNow
            };
            var bindings = mappings.Select(binding => new TemplateFieldBindingModel
            {
                id = Guid.NewGuid(),
                revision_id = revision.id,
                placeholder = binding.Placeholder,
                form_node_id = binding.FormNodeId
            }).ToArray();
            await store.StageRevisionAsync(revision, bindings, token);
            await store.StageLatestContentAsync(templateId, bytes, token);
            await Security.SaveChangesAsync(token);
            return new RtfTemplateRevisionInfo(revision.id, revision.revision, revision.content_type, revision.created_at);
        }, ct);
        return new(true, created);
    }
}
