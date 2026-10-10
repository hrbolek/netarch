using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>JSON schema and document projection. All database reads are filtered through the current actor's security layer.</summary>
public static class FormDocumentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/digital-processes/FormVersionResponse/schema/{id:guid}", Schema)
            .WithName("FormVersionJsonSchema").WithTags("DigitalProcesses.FormDocuments")
            .Produces<JsonObject>(200, "application/json")
            .WithDescription("Builds a JSON Schema representation of a form version from its saved FormNode definitions. Repeated sections produce arrays.");
        endpoints.MapGet("/digital-processes/SubmissionResponse/data/{id:guid}", Data)
            .WithName("SubmissionJsonData").WithTags("DigitalProcesses.FormDocuments")
            .Produces<JsonObject>(200, "application/json")
            .WithDescription("Reconstructs a submission's JSON document from the saved SubmissionNode tree, including repeated sections and nested keys.");
        endpoints.MapGet("/digital-processes/FormResponse/data/{id:guid}", DataList)
            .WithName("FormJsonSubmissionDataList").WithTags("DigitalProcesses.FormDocuments")
            .Produces<JsonArray>(200, "application/json")
            .WithDescription("Returns reconstructed JSON data of readable submissions belonging to the given form definition across all its readable versions. The id is FormDefinitionModel.id, not FormVersionModel.id.");
    }

    private static async Task<IResult> Schema(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        var version = await security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Read, ct);
        if (version is null) return Results.NotFound();
        var definitions = security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(n => n.form_version_id == id).ToArray();
        var typeIds = definitions.Select(x => x.node_type_id).Distinct().ToArray();
        var types = security.Query<FormNodeTypeModel>(SecurityOperations.Read)
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
        return Results.Json(schema);
    }

    private static async Task<IResult> Data(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        var submission = await security.GetByIdAsync<SubmissionModel>(id, SecurityOperations.Read, ct);
        if (submission is null) return Results.NotFound();
        return Results.Json(Reconstruct(submission, security));
    }

    private static async Task<IResult> DataList(Guid id, ISecurityLayer security, CancellationToken ct)
    {
        // The route id identifies the form definition, not a submission or a form version.
        var form = await security.GetByIdAsync<FormDefinitionModel>(id, SecurityOperations.Read, ct);
        if (form is null) return Results.NotFound();

        // Use security-filtered queries at all levels of the relationship.
        var versionIds = security.Query<FormVersionModel>(SecurityOperations.Read)
            .Where(version => version.form_definition_id == id)
            .Select(version => version.id)
            .ToArray();
        var submissions = security.Query<SubmissionModel>(SecurityOperations.Read)
            .Where(submission => versionIds.Contains(submission.form_version_id))
            .OrderBy(submission => submission.id)
            .ToArray();

        var result = new JsonArray();
        foreach (var submission in submissions)
            result.Add(Reconstruct(submission, security));
        return Results.Json(result);
    }

    private static JsonObject Reconstruct(SubmissionModel submission, ISecurityLayer security)
    {
        var nodes = security.Query<SubmissionNodeModel>(SecurityOperations.Read)
            .Where(n => n.submission_id == submission.id).ToArray();
        var definitions = security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(n => n.form_version_id == submission.form_version_id).ToDictionary(n => n.id);
        var byParent = nodes.ToLookup(n => n.parent_id);
        JsonObject BuildChildren(Guid? parent)
        {
            var output = new JsonObject();
            var children = byParent[parent].OrderBy(n => n.position).ThenBy(n => n.occurrence).ToArray();
            if (children.Length == 0) return output;
            foreach (var group in children.Where(n => definitions.ContainsKey(n.form_node_id))
                .GroupBy(n => n.form_node_id))
            {
                var definition = definitions[group.Key];
                if (string.IsNullOrWhiteSpace(definition.key)) continue;
                JsonNode? Value(SubmissionNodeModel n)
                {
                    if (byParent[n.id].Any()) return BuildChildren(n.id);
                    if (string.IsNullOrWhiteSpace(n.value_json)) return null;
                    try { return JsonNode.Parse(n.value_json); }
                    catch (JsonException) { return JsonValue.Create(n.value_json); }
                }
                var items = group.OrderBy(n => n.occurrence).ToArray();
                if (definition.max_occurs > 1 || items.Length > 1)
                {
                    var array = new JsonArray();
                    foreach (var item in items) array.Add(Value(item));
                    output[definition.key] = array;
                }
                else output[definition.key] = Value(items[0]);
            }
            return output;
        }
        return BuildChildren(null);
    }
}
