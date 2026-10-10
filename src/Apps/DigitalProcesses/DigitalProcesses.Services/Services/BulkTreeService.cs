using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IBulkTreeService
{
    Task<FormTreeResponse> UpsertFormTreeAsync(Guid versionId, BulkFormTreeRequest request, CancellationToken ct = default);
    Task<SubmissionTreeResponse> UpdateSubmissionValuesAsync(Guid submissionId, BulkSubmissionValuesRequest request, CancellationToken ct = default);
}

/// <summary>
/// Batch, all-or-nothing commands for UIs and delegated AI agents. Omission never means deletion.
/// The caller's identity is resolved by ISecurityLayer, not supplied in the request body.
/// </summary>
public sealed class BulkTreeService(ISecurityLayer security) : IBulkTreeService
{
    public async Task<FormTreeResponse> UpsertFormTreeAsync(Guid versionId, BulkFormTreeRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.nodes);
        var version = await security.GetByIdAsync<FormVersionModel>(versionId, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Form version is not editable.");
        if (version.is_published) throw new InvalidOperationException("Published form version is immutable.");

        // Validate every requested node and its authorization before staging any writes.
        var existing = security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == versionId).ToArray();
        var byId = existing.ToDictionary(x => x.id);
        var flattened = new List<(BulkFormTreeNode Input, Guid Id, Guid? Parent, string Path, Guid Rbac)>();
        var usedIds = new HashSet<Guid>();
        var siblingKeys = new HashSet<(Guid? parent, string key)>();
        foreach (var item in existing) siblingKeys.Add((item.parent_id, item.key));

        async Task Visit(BulkFormTreeNode input, Guid? parentId, string? parentPath, Guid parentRbac)
        {
            if (string.IsNullOrWhiteSpace(input.key)) throw new ArgumentException("Form-node key is required.");
            if (input.min_occurs < 0 || input.max_occurs < 1 || input.min_occurs > input.max_occurs)
                throw new ArgumentException("Invalid occurrence limits.");
            if (input.configuration_json is not null) { using var _ = JsonDocument.Parse(input.configuration_json); }
            var type = await security.GetByIdAsync<FormNodeTypeModel>(input.node_type_id, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Form-node type is not readable.");
            if (input.max_occurs > 1 && !type.can_repeat) throw new InvalidOperationException("Type cannot repeat.");
            if ((input.children?.Count ?? 0) > 0 && !type.can_have_children)
                throw new InvalidOperationException("Type cannot contain children.");
            var key = input.key.Trim();
            Guid id;
            Guid rbac;
            string path;
            if (input.id is Guid existingId)
            {
                if (!usedIds.Add(existingId)) throw new ArgumentException("Duplicate node identifier.");
                var node = await security.GetByIdAsync<FormNodeModel>(existingId, SecurityOperations.Update, ct)
                    ?? throw new KeyNotFoundException("Form node is not editable.");
                if (node.form_version_id != versionId || node.parent_id != parentId)
                    throw new InvalidOperationException("Moving nodes is not supported by this batch command.");
                // Structural security boundaries must be modified by dedicated authorized operations.
                if (node.node_type_id != input.node_type_id)
                    throw new InvalidOperationException("Changing node type requires a dedicated command.");
                id = node.id; path = node.path; rbac = node.rbacobject_id;
                siblingKeys.Remove((node.parent_id, node.key));
            }
            else
            {
                id = Guid.NewGuid(); rbac = parentRbac;
                path = parentPath is null ? MaterializedPath.Root(id) : MaterializedPath.Child(parentPath, id);
            }
            if (!siblingKeys.Add((parentId, key))) throw new InvalidOperationException("Duplicate sibling key.");
            flattened.Add((input, id, parentId, path, rbac));
            if (input.children is not null)
                foreach (var child in input.children) await Visit(child, id, path, rbac);
        }
        foreach (var node in request.nodes) await Visit(node, null, null, version.rbacobject_id);
        var now = DateTimeOffset.UtcNow;
        await security.ExecuteInTransactionAsync(async token =>
        {
            foreach (var (input, id, parent, path, rbac) in flattened)
            {
                if (input.id is Guid oldId)
                {
                    var current = byId.TryGetValue(oldId, out var visible) ? visible :
                        throw new KeyNotFoundException("Form node is not visible.");
                    var updated = current with {
                        key = input.key.Trim(), label = input.label, description = input.description,
                        position = input.position, min_occurs = input.min_occurs, max_occurs = input.max_occurs,
                        configuration_json = input.configuration_json,
                        updated_at = now, updated_by = security.CurrentUserId
                    };
                    await security.StageUpdateAsync(updated, token);
                }
                else
                {
                    var created = new FormNodeModel(id, versionId, parent, path,
                        input.key.Trim(), input.label, input.description, input.node_type_id,
                        input.position, input.min_occurs, input.max_occurs, false,
                        input.configuration_json, rbac, now, now, security.CurrentUserId, security.CurrentUserId);
                    await security.StageCreateAsync(created, token);
                }
            }
            await security.SaveChangesAsync(token);
        }, ct);
        var rows = security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == versionId).OrderBy(x => x.path).ThenBy(x => x.position)
            .Select(FormNodeResponse.Projection).ToArray();
        return new FormTreeResponse(FormVersionResponse.FromModel(version), rows);
    }

    public async Task<SubmissionTreeResponse> UpdateSubmissionValuesAsync(Guid submissionId, BulkSubmissionValuesRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.nodes);
        var submission = await security.GetByIdAsync<SubmissionModel>(submissionId, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Submission is not editable.");
        if (submission.is_archived) throw new InvalidOperationException("Archived submission is immutable.");
        var ids = new HashSet<Guid>();
        var staged = new List<SubmissionNodeModel>();
        foreach (var input in request.nodes)
        {
            if (!ids.Add(input.id)) throw new ArgumentException("Duplicate submission node.");
            var node = await security.GetByIdAsync<SubmissionNodeModel>(input.id, SecurityOperations.Update, ct)
                ?? throw new KeyNotFoundException("Submission node is not editable.");
            if (node.submission_id != submissionId) throw new InvalidOperationException("Cross-submission update rejected.");
            var formNode = await security.GetByIdAsync<FormNodeModel>(node.form_node_id, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Form definition is not readable.");
            var type = await security.GetByIdAsync<FormNodeTypeModel>(formNode.node_type_id, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Form-node type is not readable.");
            if (!type.can_have_value) throw new InvalidOperationException("This form node cannot hold a value.");
            if (input.value_json is not null) { using var _ = JsonDocument.Parse(input.value_json); }
            staged.Add(node with {
                value_json = input.value_json,
                updated_at = DateTimeOffset.UtcNow,
                updated_by = security.CurrentUserId
            });
        }
        await security.ExecuteInTransactionAsync(async token =>
        {
            foreach (var node in staged) await security.StageUpdateAsync(node, token);
            await security.SaveChangesAsync(token);
        }, ct);
        var rows = security.Query<SubmissionNodeModel>(SecurityOperations.Read)
            .Where(x => x.submission_id == submissionId).OrderBy(x => x.path).ThenBy(x => x.position)
            .Select(SubmissionNodeResponse.Projection).ToArray();
        return new SubmissionTreeResponse(SubmissionResponse.FromModel(submission), rows);
    }
}
