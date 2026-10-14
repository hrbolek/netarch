using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Starter content of the first draft versions. Staged with the trusted layer because the freshly created
/// RBAC scope has no effective-access projection yet.
/// </summary>
internal static class DefaultVersionContent
{
    public static async Task StageFormAsync(IDBLayer db, Guid formVersionId, Guid rbacobjectId, DateTimeOffset now, Guid actor, CancellationToken ct)
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

    public static async Task StageWorkflowAsync(IDBLayer db, Guid workflowVersionId, Guid rbacobjectId, DateTimeOffset now, Guid actor, CancellationToken ct)
    {
        var draftId = Guid.NewGuid();
        var submittedId = Guid.NewGuid();
        await db.StageCreateAsync(new WorkflowStateModel(
            draftId, workflowVersionId, "draft", "Rozpracováno", null, true, false, 0, rbacobjectId, now, now, actor, actor), ct);
        await db.StageCreateAsync(new WorkflowStateModel(
            submittedId, workflowVersionId, "submitted", "Odesláno", null, false, false, 1, rbacobjectId, now, now, actor, actor), ct);
        await db.StageCreateAsync(new WorkflowTransitionModel(
            Guid.NewGuid(), workflowVersionId, draftId, submittedId, "submit", "Odeslat", false, null, null, rbacobjectId, now, now, actor, actor), ct);
        await db.StageCreateAsync(new WorkflowTransitionModel(
            Guid.NewGuid(), workflowVersionId, submittedId, draftId, "return", "Vrátit", true, null, null, rbacobjectId, now, now, actor, actor), ct);
    }
}
