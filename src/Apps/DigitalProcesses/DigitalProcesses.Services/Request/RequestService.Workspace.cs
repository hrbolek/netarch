using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.ProcessDefinitionVersion;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowState;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowTransition;
using WebAppArchitecture.DigitalProcesses.Models.WorkflowVersion;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.Request;

public sealed partial class RequestService
{
    public async Task<RequestResponse?> GetViewAsync(Guid requestId, CancellationToken ct = default)
    {
        var request = await Security.GetByIdAsync<RequestModel>(requestId, SecurityOperations.Read, ct);
        if (request is null) return null;
        var detail = await GetDetailAsync(requestId, ct);
        var process = await Security.GetByIdAsync<ProcessDefinitionVersionModel>(
            request.process_definition_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Process version not found or not readable.");
        var form = await Security.GetByIdAsync<FormVersionModel>(process.form_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Form version not found or not readable.");
        var workflow = await Security.GetByIdAsync<WorkflowVersionModel>(process.workflow_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Workflow version not found or not readable.");
        var nodes = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(node => node.form_version_id == form.id)
            .Select(FormNodeResponse.Projection).ToArray();
        var typeIds = nodes.Select(node => node.node_type_id).Distinct().ToArray();
        var types = Security.Query<FormNodeTypeModel>(SecurityOperations.Read)
            .Where(type => typeIds.Contains(type.id)).Select(FormNodeTypeResponse.Projection).ToArray();
        var states = Security.Query<WorkflowStateModel>(SecurityOperations.Read)
            .Where(state => state.workflow_version_id == workflow.id)
            .Select(WorkflowStateResponse.Projection).ToArray();
        var transitions = Security.Query<WorkflowTransitionModel>(SecurityOperations.Read)
            .Where(transition => transition.workflow_version_id == workflow.id)
            .Select(WorkflowTransitionResponse.Projection).ToArray();
        var editableSubmission = !detail.active_submission.submission.is_archived &&
            await Security.GetByIdAsync<SubmissionModel>(request.current_submission_id, SecurityOperations.Update, ct) is not null;
        var editableNodes = editableSubmission
            ? Security.Query<SubmissionNodeModel>(SecurityOperations.Update)
                .Where(node => node.submission_id == request.current_submission_id)
                .Select(node => node.id).ToArray()
            : [];
        var mayTransition = editableSubmission &&
            await Security.GetByIdAsync<RequestModel>(requestId, SecurityOperations.Update, ct) is not null &&
            await Security.GetByIdAsync<GroupModel>(request.rbacobject_id, SecurityOperations.Update, ct) is not null;
        return detail.request with
        {
            workspace = new(detail, new(FormVersionResponse.FromModel(form), nodes, types, []),
                new(WorkflowVersionResponse.FromModel(workflow), states, transitions), editableNodes, mayTransition)
        };
    }
}
