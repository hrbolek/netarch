using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using WebAppArchitecture.DigitalProcesses.Services.Shared;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormVersion;

/// <summary>Stages starter content before the new RBAC scope has an effective-access projection.</summary>
internal static class FormVersionDefaultContent
{
    public static async Task StageAsync(IDBLayer db, Guid formVersionId, Guid rbacobjectId, DateTimeOffset now, Guid actor, CancellationToken ct)
    {
        var sectionId = Guid.NewGuid();
        var textId = Guid.NewGuid();
        await db.StageCreateAsync(new FormNodeModel(
            sectionId, formVersionId, null, MaterializedPath.Root(sectionId), "section", "Sekce", null,
            DigitalProcessWellKnown.FormNodeTypes.Section, 0, 1, 1, false, null, rbacobjectId, now, now, actor, actor), ct);
        await db.StageCreateAsync(new FormNodeModel(
            textId, formVersionId, sectionId, MaterializedPath.Child(MaterializedPath.Root(sectionId), textId), "text", "Text", null,
            DigitalProcessWellKnown.FormNodeTypes.Text, 0, 1, 1, false, null, rbacobjectId, now, now, actor, actor), ct);
    }
}
