using System.Text.Json;
using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>Builds a submission JSON document from already-authorized node snapshots.</summary>
public static class SubmissionDocumentReconstructor
{
    public static JsonObject Reconstruct(
        IReadOnlyCollection<SubmissionNodeModel> nodes,
        IReadOnlyCollection<FormNodeModel> definitions)
    {
        var definitionsById = definitions.ToDictionary(node => node.id);
        var byParent = nodes.ToLookup(node => node.parent_id);

        JsonObject BuildChildren(Guid? parent)
        {
            var output = new JsonObject();
            var children = byParent[parent].OrderBy(node => node.position).ThenBy(node => node.occurrence).ToArray();
            if (children.Length == 0) return output;
            foreach (var group in children.Where(node => definitionsById.ContainsKey(node.form_node_id))
                .GroupBy(node => node.form_node_id))
            {
                var definition = definitionsById[group.Key];
                if (string.IsNullOrWhiteSpace(definition.key)) continue;

                JsonNode? Value(SubmissionNodeModel node)
                {
                    if (byParent[node.id].Any()) return BuildChildren(node.id);
                    if (string.IsNullOrWhiteSpace(node.value_json)) return null;
                    try { return JsonNode.Parse(node.value_json); }
                    catch (JsonException) { return JsonValue.Create(node.value_json); }
                }

                var items = group.OrderBy(node => node.occurrence).ToArray();
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
