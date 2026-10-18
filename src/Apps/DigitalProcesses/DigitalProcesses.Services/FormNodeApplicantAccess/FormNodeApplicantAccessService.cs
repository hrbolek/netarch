using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeApplicantAccess;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNodeApplicantAccess;

public sealed class FormNodeApplicantAccessService(IAuthorizationLayer security, IDBLayer db)
    : SecuredEntityService<FormNodeApplicantAccessModel>(security, db), IFormNodeApplicantAccessService
{
    public AuxiliaryOperationResult List(Guid processVersionId, Guid? stateId)
    {
        var process = Security.Query<ProcessDefinitionVersionModel>(SecurityOperations.Read)
            .SingleOrDefault(x => x.id == processVersionId);
        if (process is null) return AuxiliaryOperationResult.NotFound();
        if (stateId is Guid requestedStateId && !Security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Any(x => x.id == requestedStateId && x.workflow_version_id == process.workflow_version_id))
            return AuxiliaryOperationResult.NotFound();
        var query = Security.Query<FormNodeApplicantAccessModel>(SecurityOperations.Read)
            .Where(x => x.process_definition_version_id == processVersionId);
        if (stateId is Guid selectedStateId)
            query = query.Where(x => x.workflow_state_id == selectedStateId);
        var rules = query.Select(x => new { x.workflow_state_id, x.form_node_id, x.can_read }).ToArray();
        return AuxiliaryOperationResult.Json(rules);
    }

    public async Task<AuxiliaryOperationResult> SetAsync(Guid processVersionId,
        SetApplicantVisibilityRequest body, CancellationToken ct)
    {
        var process = await Security.GetByIdAsync<ProcessDefinitionVersionModel>(processVersionId, SecurityOperations.Update, ct);
        if (process is null) return AuxiliaryOperationResult.NotFound();
        if (process.is_published) return AuxiliaryOperationResult.Conflict("Published process versions are immutable.");
        if (body.StateId == Guid.Empty || body.FormNodeId == Guid.Empty)
            return AuxiliaryOperationResult.BadRequest("stateId and formNodeId must be valid, non-empty GUIDs.");
        var state = Security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .SingleOrDefault(x => x.id == body.StateId && x.workflow_version_id == process.workflow_version_id);
        var node = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .SingleOrDefault(x => x.id == body.FormNodeId && x.form_version_id == process.form_version_id);
        if (state is null || node is null) return AuxiliaryOperationResult.BadRequest("State and FormNode must belong to this process version.");
        var existing = Security.Query<FormNodeApplicantAccessModel>(SecurityOperations.Update)
            .SingleOrDefault(x => x.process_definition_version_id == processVersionId &&
                                  x.workflow_state_id == body.StateId && x.form_node_id == body.FormNodeId);
        var now = DateTimeOffset.UtcNow;
        return await Security.ExecuteInTransactionAsync(async token =>
        {
            if (existing is null)
                await Security.StageCreateAsync(new FormNodeApplicantAccessModel(
                    Guid.NewGuid(), processVersionId, body.StateId, body.FormNodeId, body.CanRead,
                    process.rbacobject_id, now, now, Security.CurrentUserId, Security.CurrentUserId), token);
            else
                await Security.StageUpdateAsync(existing with
                {
                    can_read = body.CanRead, updated_at = now, updated_by = Security.CurrentUserId
                }, token);
            await Security.SaveChangesAsync(token);
            return AuxiliaryOperationResult.NoContent();
        }, ct);
    }
}
