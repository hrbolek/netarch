using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using System.Text.Json;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.Submission;

public sealed partial class SubmissionService
{
    public async Task<SubmissionTreeResponse> UpdateValuesAsync(Guid submissionId, BulkSubmissionValuesRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.nodes);
        var submission = await Security.GetByIdAsync<SubmissionModel>(submissionId, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Submission is not editable.");
        if (submission.is_archived) throw new InvalidOperationException("Archived submission is immutable.");
        var ids = new HashSet<Guid>();
        var staged = new List<SubmissionNodeModel>();
        foreach (var input in request.nodes)
        {
            if (!ids.Add(input.id)) throw new ArgumentException("Duplicate submission node.");
            var node = await Security.GetByIdAsync<SubmissionNodeModel>(input.id, SecurityOperations.Update, ct)
                ?? throw new KeyNotFoundException("Submission node is not editable.");
            if (node.submission_id != submissionId) throw new InvalidOperationException("Cross-submission update rejected.");
            var formNode = await Security.GetByIdAsync<FormNodeModel>(node.form_node_id, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Form definition is not readable.");
            var type = await Security.GetByIdAsync<FormNodeTypeModel>(formNode.node_type_id, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Form-node type is not readable.");
            if (!type.can_have_value) throw new InvalidOperationException("This form node cannot hold a value.");
            if (input.value_json is not null) { using var _ = JsonDocument.Parse(input.value_json); }
            staged.Add(node with {
                value_json = input.value_json,
                updated_at = DateTimeOffset.UtcNow,
                updated_by = Security.CurrentUserId
            });
        }
        await Security.ExecuteInTransactionAsync(async token =>
        {
            foreach (var node in staged) await Security.StageUpdateAsync(node, token);
            await Security.SaveChangesAsync(token);
        }, ct);
        var rows = Security.Query<SubmissionNodeModel>(SecurityOperations.Read)
            .Where(x => x.submission_id == submissionId).OrderBy(x => x.path).ThenBy(x => x.position)
            .Select(SubmissionNodeResponse.Projection).ToArray();
        return new SubmissionTreeResponse(SubmissionResponse.FromModel(submission), rows);
    }
}
