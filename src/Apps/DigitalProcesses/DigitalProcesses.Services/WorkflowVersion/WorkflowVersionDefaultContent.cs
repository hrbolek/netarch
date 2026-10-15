using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.WorkflowVersion;

/// <summary>Stages starter content before the new RBAC scope has an effective-access projection.</summary>
internal static class WorkflowVersionDefaultContent
{
    public static async Task StageAsync(IDBLayer db, Guid workflowVersionId, Guid rbacobjectId, DateTimeOffset now, Guid actor, CancellationToken ct)
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
