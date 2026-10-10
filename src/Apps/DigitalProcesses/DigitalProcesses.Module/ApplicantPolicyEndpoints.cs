using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>
/// Applicant visibility is configured in the context of a DRAFT process version,
/// not on the reusable form definition itself. Publishing freezes the rules.
/// </summary>
public static class ApplicantPolicyEndpoints
{
    public sealed record SetApplicantVisibilityRequest(Guid StateId, Guid FormNodeId, bool CanRead);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/digital-processes/ProcessDefinitionVersionResponse/applicant-access")
            .WithTags("DigitalProcesses.ApplicantVisibility");
        group.MapGet("/{processVersionId:guid}", List).WithName("ApplicantAccessList")
            .WithDescription("Lists Applicant visibility rules for the process version. Optional query parameter stateId filters by state.");
        group.MapPut("/{processVersionId:guid}", Set).WithName("ApplicantAccessSet")
            .WithDescription("Sets Applicant visibility for a node in a workflow state of a draft process version. Pass stateId, formNodeId and canRead in the JSON body. Requires Update on the process version.");
    }

    private static IResult List(Guid processVersionId, Guid? stateId, ISecurityLayer security)
    {
        var process = security.Query<ProcessDefinitionVersionModel>(SecurityOperations.Read)
            .SingleOrDefault(x => x.id == processVersionId);
        if (process is null) return Results.NotFound();
        if (stateId is Guid requestedStateId && !security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Any(x => x.id == requestedStateId && x.workflow_version_id == process.workflow_version_id))
            return Results.NotFound();
        var query = security.Query<FormNodeApplicantAccessModel>(SecurityOperations.Read)
            .Where(x => x.process_definition_version_id == processVersionId);
        if (stateId is Guid selectedStateId)
            query = query.Where(x => x.workflow_state_id == selectedStateId);
        var rules = query.Select(x => new { x.workflow_state_id, x.form_node_id, x.can_read }).ToArray();
        return Results.Json(rules);
    }

    private static async Task<IResult> Set(Guid processVersionId,
        SetApplicantVisibilityRequest body, ISecurityLayer security, CancellationToken ct)
    {
        var process = await security.GetByIdAsync<ProcessDefinitionVersionModel>(processVersionId, SecurityOperations.Update, ct);
        if (process is null) return Results.NotFound();
        if (process.is_published) return Results.Conflict("Published process versions are immutable.");
        if (body.StateId == Guid.Empty || body.FormNodeId == Guid.Empty)
            return Results.BadRequest("stateId and formNodeId must be valid, non-empty GUIDs.");
        var state = security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .SingleOrDefault(x => x.id == body.StateId && x.workflow_version_id == process.workflow_version_id);
        var node = security.Query<FormNodeModel>(SecurityOperations.Read)
            .SingleOrDefault(x => x.id == body.FormNodeId && x.form_version_id == process.form_version_id);
        if (state is null || node is null) return Results.BadRequest("State and FormNode must belong to this process version.");
        var existing = security.Query<FormNodeApplicantAccessModel>(SecurityOperations.Update)
            .SingleOrDefault(x => x.process_definition_version_id == processVersionId &&
                                  x.workflow_state_id == body.StateId && x.form_node_id == body.FormNodeId);
        var now = DateTimeOffset.UtcNow;
        return await security.ExecuteInTransactionAsync(async token =>
        {
            if (existing is null)
                await security.StageCreateAsync(new FormNodeApplicantAccessModel(
                    Guid.NewGuid(), processVersionId, body.StateId, body.FormNodeId, body.CanRead,
                    process.rbacobject_id, now, now, security.CurrentUserId, security.CurrentUserId), token);
            else
                await security.StageUpdateAsync(existing with
                {
                    can_read = body.CanRead, updated_at = now, updated_by = security.CurrentUserId
                }, token);
            await security.SaveChangesAsync(token);
            return Results.NoContent();
        }, ct);
    }
}
