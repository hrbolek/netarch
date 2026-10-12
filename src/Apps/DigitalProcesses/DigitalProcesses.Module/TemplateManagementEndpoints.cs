using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>
/// Versioned RTF template management. Revision bytes and field bindings are immutable;
/// edits create a new revision. AI tools may submit proposed bindings through this API.
/// </summary>
public static class TemplateManagementEndpoints
{
    public sealed record FieldBindingRequest(string Placeholder, Guid FormNodeId);
    public sealed record NewRevisionRequest(string RtfBase64, FieldBindingRequest[]? Bindings);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        const string prefix = "/digital-processes/document-templates";
        endpoints.MapGet(prefix + "/{id:guid}/revisions", Revisions)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("List immutable revisions of a template; requires Read on its FormVersion.");
        endpoints.MapGet(prefix + "/revisions/{revisionId:guid}/content", DownloadRevision)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("Download an exact immutable RTF revision.");
        endpoints.MapPost(prefix + "/{id:guid}/revisions", CreateRevision)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("Create a new RTF revision with verified FormNode bindings; requires Update on FormVersion.");
        endpoints.MapGet(prefix + "/form-schema/{formVersionId:guid}", FormSchema)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("Schema context for future AI template design. Returns only readable FormNodes.");
    }

    private static async Task<IResult> Revisions(Guid id, DigitalProcessesDbContext db, ISecurityLayer security, CancellationToken ct)
    {
        var template = await db.DocumentTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.id == id, ct);
        if (template is null || await security.GetByIdAsync<FormVersionModel>(template.form_version_id, SecurityOperations.Read, ct) is null)
            return Results.NotFound();
        var revisions = await db.DocumentTemplateRevisions.AsNoTracking()
            .Where(x => x.template_id == id).OrderBy(x => x.revision)
            .Select(x => new { x.id, x.revision, x.content_type, x.created_at }).ToListAsync(ct);
        return Results.Ok(revisions);
    }

    private static async Task<IResult> DownloadRevision(Guid revisionId, DigitalProcessesDbContext db, ISecurityLayer security, CancellationToken ct)
    {
        var revision = await db.DocumentTemplateRevisions.AsNoTracking().SingleOrDefaultAsync(x => x.id == revisionId, ct);
        if (revision is null) return Results.NotFound();
        var template = await db.DocumentTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.id == revision.template_id, ct);
        if (template is null || await security.GetByIdAsync<FormVersionModel>(template.form_version_id, SecurityOperations.Read, ct) is null)
            return Results.NotFound();
        return Results.File(revision.content, revision.content_type, $"template-{revision.template_id:N}-r{revision.revision}.rtf");
    }

    private static async Task<IResult> FormSchema(Guid formVersionId, ISecurityLayer security, CancellationToken ct)
    {
        if (await security.GetByIdAsync<FormVersionModel>(formVersionId, SecurityOperations.Read, ct) is null)
            return Results.NotFound();
        var nodes = security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == formVersionId)
            .Select(x => new { x.id, x.key, x.label, x.description, x.node_type_id }).ToArray();
        return Results.Ok(nodes);
    }

    private static async Task<IResult> CreateRevision(Guid id, NewRevisionRequest request, DigitalProcessesDbContext db,
        ISecurityLayer security, CancellationToken ct)
    {
        var template = await db.DocumentTemplates.SingleOrDefaultAsync(x => x.id == id, ct);
        if (template is null || await security.GetByIdAsync<FormVersionModel>(template.form_version_id, SecurityOperations.Update, ct) is null)
            return Results.NotFound();
        byte[] bytes;
        try { bytes = Convert.FromBase64String(request.RtfBase64); }
        catch (FormatException) { return Results.BadRequest(new { error = "Invalid Base64." }); }
        if (bytes.Length == 0 || bytes.Length > 5_000_000 || !RtfTemplateEngine.IsRtf(bytes) || !RtfTemplateEngine.IsValidTemplate(bytes, out _))
            return Results.BadRequest(new { error = "Invalid RTF or size exceeds 5 MB." });
        var mappings = request.Bindings ?? Array.Empty<FieldBindingRequest>();
        if (mappings.Any(x => string.IsNullOrWhiteSpace(x.Placeholder)) ||
            mappings.Select(x => x.Placeholder).Distinct(StringComparer.Ordinal).Count() != mappings.Length)
            return Results.BadRequest(new { error = "Placeholders must be nonempty and unique." });
        var nodeIds = mappings.Select(x => x.FormNodeId).Distinct().ToArray();
        var validIds = await db.FormNodes.AsNoTracking()
            .Where(x => x.form_version_id == template.form_version_id && nodeIds.Contains(x.id))
            .Select(x => x.id).ToListAsync(ct);
        if (validIds.Count != nodeIds.Length ||
            security.Query<FormNodeModel>(SecurityOperations.Read)
                .Count(x => nodeIds.Contains(x.id) && x.form_version_id == template.form_version_id) != nodeIds.Length)
            return Results.BadRequest(new { error = "A binding references an absent or unreadable FormNode." });

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var next = (await db.DocumentTemplateRevisions.Where(x => x.template_id == id)
            .MaxAsync(x => (int?)x.revision, ct) ?? 0) + 1;
        var revision = new DocumentTemplateRevisionModel {
            id = Guid.NewGuid(), template_id = id, revision = next, content = bytes,
            created_at = DateTimeOffset.UtcNow
        };
        db.DocumentTemplateRevisions.Add(revision);
        foreach (var binding in mappings)
            db.TemplateFieldBindings.Add(new TemplateFieldBindingModel {
                id = Guid.NewGuid(), revision_id = revision.id,
                placeholder = binding.Placeholder, form_node_id = binding.FormNodeId
            });
        // Keep the legacy rendering endpoint on the newest revision.
        template.content = bytes;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/digital-processes/document-templates/revisions/{revision.id}/content",
            new { revision.id, revision.revision });
    }
}
