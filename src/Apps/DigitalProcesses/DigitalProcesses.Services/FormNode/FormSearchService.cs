using WebAppArchitecture.DigitalProcesses.Services.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using System.Text.Json.Nodes;
using System.Text.Json;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNode;

public sealed class FormSearchService(IFormNodeService nodes, IFormNodeTypeService types, IFormCalculationService calculations, IFormNodeInsertionService insertion) : IFormSearchService
{
    public async Task<AuxiliaryOperationResult> SearchAsync(FormSearchRequest request, CancellationToken ct)
    {
        if (request.NodeId == Guid.Empty || string.IsNullOrWhiteSpace(request.Pattern) || request.Pattern.Length > 200)
            return AuxiliaryOperationResult.BadRequest(new { error = "A form node and search pattern of at most 200 characters are required." });

        var node = await nodes.GetByIdAsync<FormNodeResponse>(request.NodeId, ct);
        if (node is null)
            return AuxiliaryOperationResult.NotFound();
        if (request.Values is { Count: > 400 })
            return AuxiliaryOperationResult.BadRequest(new { error = "At most 400 current form values can be supplied." });

        var type = await types.GetByIdAsync<FormNodeTypeResponse>(node.node_type_id, ct);
        if (type?.renderer_key != "input.graphql-search")
            return AuxiliaryOperationResult.NotFound();

        JsonObject configuration;
        try
        {
            configuration = FormNodeConfiguration.Parse(node.configuration_json);
        }
        catch (JsonException ex)
        {
            return AuxiliaryOperationResult.Problem(statusCode: 422,
                title: "Invalid GraphQL search configuration.",
                detail: $"Edit configuration_json of form node {node.id}. {ex.Message}");
        }
        var query = ReadString(configuration, "query");
        var minLength = ReadInt(configuration, "minLength", 3);
        if (string.IsNullOrWhiteSpace(query) || minLength is < 1 or > 20)
            return AuxiliaryOperationResult.Problem(statusCode: 422,
                title: "Invalid GraphQL search configuration.",
                detail: "Configure a non-empty query string and minLength between 1 and 20.");

        if (request.Pattern.Trim().Length < minLength)
            return AuxiliaryOperationResult.Ok(new { data = new { choices = Array.Empty<SearchOption>() } });

        var form = await nodes.GetDesignerAsync(node.form_version_id, ct);
        return await ExecuteAsync(request, configuration, query, form.nodes, ct);
    }

    public async Task<AuxiliaryOperationResult> SearchDraftAsync(FormNodeDraftSearchRequest request, CancellationToken ct)
    {
        if (request.workspace is null)
            return AuxiliaryOperationResult.BadRequest(new { error = "A draft workspace is required." });
        if (string.IsNullOrWhiteSpace(request.pattern) || request.pattern.Length > 200 || request.values is { Count: > 400 })
            return AuxiliaryOperationResult.BadRequest(new { error = "Invalid search pattern or too many preview values." });
        var workspace = await insertion.WorkspaceAsync(request.workspace, ct);
        var node = workspace.preview_nodes.SingleOrDefault(x => x.id == request.node_id)
            ?? throw new ArgumentException("Search node does not belong to the draft.");
        if (!workspace.node_types.Any(x => x.id == node.node_type_id && x.renderer_key == "input.graphql-search"))
            throw new ArgumentException("Selected draft node is not a search renderer.");
        var configuration = FormNodeConfiguration.Parse(node.configuration_json);
        var query = ReadString(configuration, "query");
        var minLength = ReadInt(configuration, "minLength", 3);
        if (string.IsNullOrWhiteSpace(query) || minLength is < 1 or > 20)
            return AuxiliaryOperationResult.Problem(422, "Invalid GraphQL search configuration.",
                "Configure query and minLength between 1 and 20.");
        if (request.pattern.Trim().Length < minLength)
            return AuxiliaryOperationResult.Ok(new { data = new { choices = Array.Empty<SearchOption>() } });
        return await ExecuteAsync(new FormSearchRequest(request.node_id, request.pattern, request.values),
            configuration, query, workspace.preview_nodes, ct);
    }

    private async Task<AuxiliaryOperationResult> ExecuteAsync(
        FormSearchRequest request, JsonObject configuration, string query,
        IReadOnlyCollection<FormNodeResponse> definitions, CancellationToken ct)
    {
        IReadOnlyDictionary<string, JsonNode?> formValues;
        try
        {
            formValues = BuildFormValues(request.Values ?? Array.Empty<FormSearchValue>(), definitions);
        }
        catch (ArgumentException ex)
        {
            return AuxiliaryOperationResult.BadRequest(new { error = ex.Message });
        }
        var variables = configuration["variables"] as JsonObject is { } configuredVariables
            ? configuredVariables.DeepClone() as JsonObject ?? new JsonObject()
            : new JsonObject();
        variables["fragment"] = JsonValue.Create(request.Pattern.Trim());
        var evaluationConfiguration = new JsonObject
        {
            ["dataSource"] = new JsonObject
            {
                ["query"] = query,
                ["path"] = "data.choices",
                ["variables"] = variables
            }
        };
        var result = await calculations.EvaluateAsync(evaluationConfiguration, formValues, ct);
        if (result is not JsonArray rows)
            return AuxiliaryOperationResult.Problem(statusCode: 502, title: "GraphQL search did not return an array.");

        var options = new List<SearchOption>();
        foreach (var row in rows)
        {
            if (row is not JsonObject item
                || item["id"] is not JsonValue itemValue
                || ReadScalar(item["label"]) is not { Length: > 0 } itemLabel)
                return AuxiliaryOperationResult.Problem(statusCode: 502, title: "GraphQL search returned a choice without 'id' and 'label' fields. Alias the selected fields in the query as 'id' and 'label'.");
            options.Add(new SearchOption(itemValue.DeepClone(), itemLabel));
            if (options.Count == 20) break;
        }

        return AuxiliaryOperationResult.Ok(new { data = new { choices = options } });
    }

    private static string? ReadString(JsonObject source, string key)
        => source[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static int ReadInt(JsonObject source, string key, int defaultValue)
        => source[key] is JsonValue value && value.TryGetValue<int>(out var result) ? result : defaultValue;

    private static IReadOnlyDictionary<string, JsonNode?> BuildFormValues(
        IReadOnlyList<FormSearchValue> submitted,
        IReadOnlyCollection<FormNodeResponse> nodes)
    {
        var nodesById = nodes.ToDictionary(item => item.id);
        var values = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var item in submitted)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 256
                || !item.Name.StartsWith("node-", StringComparison.Ordinal))
                throw new ArgumentException("Each form value must use a node-<id> field name.");

            var name = item.Name[5..];
            var suffixStart = name.IndexOf("__", StringComparison.Ordinal);
            var nodeIdText = suffixStart < 0 ? name : name[..suffixStart];
            if (!Guid.TryParseExact(nodeIdText, "N", out var nodeId)
                || !nodesById.TryGetValue(nodeId, out var definition))
                throw new ArgumentException("A supplied form value does not belong to this form version.");

            var indexes = suffixStart < 0
                ? Array.Empty<int>()
                : ParseOccurrenceIndexes(name[(suffixStart + 2)..]);
            var path = BuildFormPath(definition, nodesById, indexes);
            var jsonValue = item.Value.ValueKind == JsonValueKind.Undefined
                ? null
                : JsonNode.Parse(item.Value.GetRawText());
            values[path] = jsonValue;
        }
        return values;
    }

    private static int[] ParseOccurrenceIndexes(string suffix)
    {
        if (suffix.Length == 0)
            throw new ArgumentException("Form value occurrence indexes cannot be empty.");
        var parts = suffix.Split('_');
        if (parts.Length > 20 || parts.Any(part => !int.TryParse(part, out var index) || index < 0 || index > 10000))
            throw new ArgumentException("Form value occurrence indexes are invalid.");
        return parts.Select(int.Parse).ToArray();
    }

    private static string BuildFormPath(
        FormNodeResponse definition,
        IReadOnlyDictionary<Guid, FormNodeResponse> nodesById,
        IReadOnlyList<int> indexes)
    {
        var ancestry = new Stack<FormNodeResponse>();
        var visited = new HashSet<Guid>();
        FormNodeResponse? current = definition;
        while (current is not null && visited.Add(current.id))
        {
            ancestry.Push(current);
            current = current.parent_id is Guid parentId && nodesById.TryGetValue(parentId, out var parent)
                ? parent
                : null;
        }
        if (current is not null)
            throw new InvalidOperationException("Form tree contains a parent cycle.");

        var indexPosition = 0;
        var segments = new List<string>();
        foreach (var ancestor in ancestry)
        {
            var segment = ancestor.key;
            if (ancestor.max_occurs > 1 && indexPosition < indexes.Count)
                segment += $"[{indexes[indexPosition++]}]";
            segments.Add(segment);
        }
        if (indexPosition != indexes.Count)
            throw new ArgumentException("Form value contains more occurrence indexes than its form hierarchy.");
        return string.Join('.', segments);
    }

    private static string? ReadScalar(JsonNode? value)
        => value is JsonValue scalar
            ? scalar.TryGetValue<string>(out var text) ? text : scalar.ToString()
            : null;

    private sealed record SearchOption(JsonNode Id, string Label);
}
