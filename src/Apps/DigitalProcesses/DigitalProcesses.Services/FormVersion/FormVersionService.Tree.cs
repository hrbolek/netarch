using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.Shared;
using System.Text.Json;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormVersion;

public sealed partial class FormVersionService
{
    public async Task<FormTreeResponse> UpsertTreeAsync(Guid versionId, BulkFormTreeRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if ((request.nodes is null) == (request.children is null))
            throw new ArgumentException("Supply exactly one of 'nodes' (bulk array) or 'children' (form definition object).");
        var version = await Security.GetByIdAsync<FormVersionModel>(versionId, SecurityOperations.Update, ct)
            ?? throw new KeyNotFoundException("Form version is not editable.");
        if (version.is_published) throw new InvalidOperationException("Published form version is immutable.");

        // Validate every requested node and its authorization before staging any writes.
        var existing = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == versionId).ToArray();
        var inputNodes = request.nodes;
        if (request.children is JsonElement children)
        {
            var schema = FormDefinitionService.ReadSchemaNodes(children,
                Security.Query<FormNodeTypeModel>(SecurityOperations.Read).ToArray());
            var siblings = existing.ToLookup(x => x.parent_id);
            IReadOnlyList<BulkFormTreeNode> Convert(
                IReadOnlyList<FormDefinitionService.SchemaNode> items, Guid? parentId, bool isNewParent = false)
                => items.Select(item =>
                {
                    var old = isNewParent ? null : siblings[parentId].SingleOrDefault(x => x.key == item.Key);
                    return new BulkFormTreeNode(old?.id, item.Key, item.NodeTypeId,
                        item.Label, item.Description, item.Position, item.MinOccurs, item.MaxOccurs,
                        item.ConfigurationJson, Convert(item.Children, old?.id, old is null));
                }).ToArray();
            inputNodes = Convert(schema, null);
        }
        var byId = existing.ToDictionary(x => x.id);
        var flattened = new List<(BulkFormTreeNode Input, Guid Id, Guid? Parent, string Path, Guid Rbac)>();
        var usedIds = new HashSet<Guid>();
        var siblingKeys = new HashSet<(Guid? parent, string key)>();
        foreach (var item in existing) siblingKeys.Add((item.parent_id, item.key));

        async Task Visit(BulkFormTreeNode input, Guid? parentId, string? parentPath, Guid parentRbac)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (string.IsNullOrWhiteSpace(input.key)) throw new ArgumentException("Form-node key is required.");
            if (input.min_occurs < 0 || input.max_occurs < 1 || input.min_occurs > input.max_occurs)
                throw new ArgumentException("Invalid occurrence limits.");
            if (input.configuration_json is not null) { using var _ = JsonDocument.Parse(input.configuration_json); }
            var type = await Security.GetByIdAsync<FormNodeTypeModel>(input.node_type_id, SecurityOperations.Read, ct)
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
                var node = await Security.GetByIdAsync<FormNodeModel>(existingId, SecurityOperations.Update, ct)
                    ?? throw new KeyNotFoundException("Form node is not editable.");
                if (node.form_version_id != versionId || node.parent_id != parentId)
                    throw new InvalidOperationException("Moving nodes is not supported by this batch command.");
                // Structural Security boundaries must be modified by dedicated authorized operations.
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
        foreach (var node in inputNodes!) await Visit(node, null, null, version.rbacobject_id);
        var types = Security.Query<FormNodeTypeModel>(SecurityOperations.Read).ToDictionary(x => x.id);
        foreach (var (input, id, parent, _, _) in flattened)
        {
            var hasSections = flattened.Any(child => child.Parent == id && types[child.Input.node_type_id].renderer_key == "section")
                || existing.Any(child => child.parent_id == id && !usedIds.Contains(child.id) && types[child.node_type_id].renderer_key == "section");
            FormSectionRules.Validate(parent, types[input.node_type_id], input.max_occurs, input.configuration_json, hasSections);
        }
        var now = DateTimeOffset.UtcNow;
        await Security.ExecuteInTransactionAsync(async token =>
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
                        updated_at = now, updated_by = Security.CurrentUserId
                    };
                    await Security.StageUpdateAsync(updated, token);
                }
                else
                {
                    var created = new FormNodeModel(id, versionId, parent, path,
                        input.key.Trim(), input.label, input.description, input.node_type_id,
                        input.position, input.min_occurs, input.max_occurs, false,
                        input.configuration_json, rbac, now, now, Security.CurrentUserId, Security.CurrentUserId);
                    await Security.StageCreateAsync(created, token);
                }
            }
            await Security.SaveChangesAsync(token);
        }, ct);
        var rows = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == versionId).OrderBy(x => x.path).ThenBy(x => x.position)
            .Select(FormNodeResponse.Projection).ToArray();
        return new FormTreeResponse(FormVersionResponse.FromModel(version), rows);
    }
}
