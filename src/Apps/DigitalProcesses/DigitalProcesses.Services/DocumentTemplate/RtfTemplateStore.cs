using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplate;
using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplateRevision;
using WebAppArchitecture.DigitalProcesses.Models.TemplateFieldBinding;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
/// <summary>
/// Persistence helper for templates. Production access is authorized by FormVersionService;
/// the temporary designer preview also uses its read-only compatibility surface.
/// </summary>
public sealed class RtfTemplateStore(IDBLayer db)
{
    public IReadOnlyList<RtfTemplateInfo> List(Guid version) => db.Query<DocumentTemplateModel>()
        .Where(x => x.form_version_id == version).OrderBy(x => x.created_at)
        .Select(x => new RtfTemplateInfo(x.id, x.form_version_id, x.name, x.created_at)).ToArray();

    public RtfTemplateInfo? Find(Guid id) => db.Query<DocumentTemplateModel>()
        .Where(x => x.id == id)
        .Select(x => new RtfTemplateInfo(x.id, x.form_version_id, x.name, x.created_at))
        .SingleOrDefault();

    public byte[] Read(Guid id) => db.Query<DocumentTemplateModel>()
        .Where(x => x.id == id).Select(x => x.content).Single();

    public async Task<RtfTemplateInfo> SaveAsync(Guid version, string name, byte[] bytes, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new DocumentTemplateModel
        {
            id = Guid.NewGuid(),
            form_version_id = version,
            name = name[..Math.Min(name.Length, 200)],
            content_type = "application/rtf",
            content = bytes,
            created_at = now
        };
        await db.StageCreateAsync(entity, ct);
        await db.StageCreateAsync(new DocumentTemplateRevisionModel
        {
            id = Guid.NewGuid(),
            template_id = entity.id,
            revision = 1,
            content = bytes.ToArray(),
            content_type = "application/rtf",
            created_at = now
        }, ct);
        return new RtfTemplateInfo(entity.id, version, entity.name, now);
    }

    public IReadOnlyList<RtfTemplateRevisionInfo> ListRevisions(Guid templateId)
        => db.Query<DocumentTemplateRevisionModel>().Where(x => x.template_id == templateId)
            .OrderBy(x => x.revision)
            .Select(x => new RtfTemplateRevisionInfo(x.id, x.revision, x.content_type, x.created_at)).ToArray();

    public DocumentTemplateRevisionModel? FindRevision(Guid revisionId)
        => db.Query<DocumentTemplateRevisionModel>().SingleOrDefault(x => x.id == revisionId);

    public DocumentTemplateModel? FindEntity(Guid id)
        => db.Query<DocumentTemplateModel>().SingleOrDefault(x => x.id == id);

    public int NextRevision(Guid templateId)
        => (db.Query<DocumentTemplateRevisionModel>().Where(x => x.template_id == templateId)
            .Select(x => (int?)x.revision).Max() ?? 0) + 1;

    public async Task StageRevisionAsync(DocumentTemplateRevisionModel revision, IEnumerable<TemplateFieldBindingModel> bindings, CancellationToken ct)
    {
        await db.StageCreateAsync(revision, ct);
        foreach (var binding in bindings)
            await db.StageCreateAsync(binding, ct);
    }

    public async Task StageLatestContentAsync(Guid templateId, byte[] content, CancellationToken ct)
    {
        var entity = FindEntity(templateId) ?? throw new KeyNotFoundException("Template not found.");
        entity.content = content;
        await db.StageUpdateAsync(entity, ct);
    }

    public async Task StageDeleteAsync(Guid templateId, CancellationToken ct)
    {
        if (FindEntity(templateId) is not null)
            await db.StageDeleteAsync<DocumentTemplateModel>(templateId, ct);
    }
}
