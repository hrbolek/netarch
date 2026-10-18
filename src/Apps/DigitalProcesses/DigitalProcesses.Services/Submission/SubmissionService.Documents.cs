using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using System.Text.Json.Nodes;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.Submission;

public sealed partial class SubmissionService
{
    public async Task<AuxiliaryOperationResult> SchemaAsync(Guid id, CancellationToken ct)
    {
        var version = await Security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Read, ct);
        if (version is null) return AuxiliaryOperationResult.NotFound();
        var definitions = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(n => n.form_version_id == id).ToArray();
        var typeIds = definitions.Select(x => x.node_type_id).Distinct().ToArray();
        var types = Security.Query<FormNodeTypeModel>(SecurityOperations.Read)
            .Where(t => typeIds.Contains(t.id)).ToDictionary(t => t.id);
        JsonObject NodeSchema(FormNodeModel node)
        {
            var children = definitions.Where(n => n.parent_id == node.id).OrderBy(n => n.position).ToArray();
            var isContainer = children.Length > 0 || (types.TryGetValue(node.node_type_id, out var t) && t.can_have_children);
            JsonObject result;
            if (isContainer)
            {
                var props = new JsonObject();
                var required = new JsonArray();
                foreach (var child in children.Where(n => !string.IsNullOrWhiteSpace(n.key)))
                {
                    props[child.key] = NodeSchema(child);
                    if (child.min_occurs > 0) required.Add(child.key);
                }
                result = new JsonObject { ["type"] = "object", ["properties"] = props };
                if (required.Count > 0) result["required"] = required;
            }
            else
            {
                var renderer = types.TryGetValue(node.node_type_id, out var kind) ? kind.renderer_key.ToLowerInvariant() : "";
                var valueType = renderer.Contains("checkbox") || renderer.Contains("boolean") ? "boolean"
                    : renderer.Contains("number") || renderer.Contains("decimal") ? "number"
                    : renderer.Contains("integer") ? "integer" : "string";
                result = new JsonObject { ["type"] = valueType };
            }
            if (!string.IsNullOrWhiteSpace(node.label)) result["title"] = node.label;
            if (!string.IsNullOrWhiteSpace(node.description)) result["description"] = node.description;
            if (node.max_occurs > 1)
                return new JsonObject { ["type"] = "array", ["items"] = result, ["minItems"] = node.min_occurs, ["maxItems"] = node.max_occurs };
            return result;
        }
        var properties = new JsonObject();
        var requiredRoot = new JsonArray();
        foreach (var root in definitions.Where(n => n.parent_id is null && !string.IsNullOrWhiteSpace(n.key)).OrderBy(n => n.position))
        {
            properties[root.key] = NodeSchema(root);
            if (root.min_occurs > 0) requiredRoot.Add(root.key);
        }
        var schema = new JsonObject { ["$schema"] = "https://json-schema.org/draft/2020-12/schema", ["type"] = "object", ["properties"] = properties };
        if (requiredRoot.Count > 0) schema["required"] = requiredRoot;
        return AuxiliaryOperationResult.Json(schema);
    }

    public async Task<AuxiliaryOperationResult> DataAsync(Guid id, CancellationToken ct)
    {
        var submission = await Security.GetByIdAsync<SubmissionModel>(id, SecurityOperations.Read, ct);
        if (submission is null) return AuxiliaryOperationResult.NotFound();
        return AuxiliaryOperationResult.Json(Reconstruct(submission.id, submission.form_version_id));
    }

    public async Task<AuxiliaryOperationResult> DataListAsync(Guid id, CancellationToken ct)
    {
        // The route id identifies a form definition, not a submission or version.
        var form = await Security.GetByIdAsync<FormDefinitionModel>(id, SecurityOperations.Read, ct);
        if (form is null) return AuxiliaryOperationResult.NotFound();
        var versionIds = Security.Query<FormVersionModel>(SecurityOperations.Read)
            .Where(version => version.form_definition_id == id)
            .Select(version => version.id)
            .ToArray();
        var submissions = Security.Query<SubmissionModel>(SecurityOperations.Read)
            .Where(submission => versionIds.Contains(submission.form_version_id))
            .OrderBy(submission => submission.id)
            .ToArray();
        var result = new JsonArray();
        foreach (var submission in submissions)
            result.Add(Reconstruct(submission.id, submission.form_version_id));
        return AuxiliaryOperationResult.Json(result);
    }

    public JsonObject Reconstruct(Guid submissionId, Guid formVersionId)
    {
        var nodes = Security.Query<SubmissionNodeModel>(SecurityOperations.Read)
            .Where(n => n.submission_id == submissionId).ToArray();
        var definitions = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(n => n.form_version_id == formVersionId).ToArray();
        return SubmissionDocumentReconstructor.Reconstruct(nodes, definitions);
    }
}
