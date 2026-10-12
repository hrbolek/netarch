using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Text.Json.Nodes;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>Read-only, approval-required AI form design proposal. Never persists agent output.</summary>
public static class FormDesignAiEndpoints
{
    public sealed record DesignRequest(Guid FormVersionId, string Instruction);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/digital-processes/ai/form-design/approve", Approve)
            .WithTags("DigitalProcesses.AI")
            .WithDescription("Apply an explicitly approved AI form proposal to an editable form version; validates components, keys and RBAC.");
        endpoints.MapPost("/digital-processes/ai/form-design", Design)
            .WithTags("DigitalProcesses.AI")
            .WithDescription("Propose a form schema via configured FastAPI/PydanticAI agent. Checks Read on form version, sends only readable nodes, returns an unpersisted proposal. Never stores agent output.");
    }

    private static async Task<IResult> Design(
        DesignRequest request,
        ISecurityLayer security,
        IHttpClientFactory clients,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var log = loggerFactory.CreateLogger("DigitalProcesses.AI.FormDesign");
        if (request.FormVersionId == Guid.Empty || string.IsNullOrWhiteSpace(request.Instruction) || request.Instruction.Length > 8000)
            return Results.BadRequest(new { error = "A form version and instruction (1-8000 characters) are required." });
        if (await security.GetByIdAsync<FormVersionModel>(request.FormVersionId, SecurityOperations.Read, ct) is null)
            return Results.NotFound();

        const string key = "formdesign_aiagent";
        var config = configuration.GetSection($"AiAgents:{key}");
        if (!config.GetValue<bool>("enabled"))
            return Results.Problem(statusCode: 503, title: "Form design AI is disabled.");
        var endpoint = config["endpoint"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(uri.UserInfo))
            return Results.Problem(statusCode: 503, title: "Form design agent endpoint is not configured.");

        // The server, not the browser, determines accessible nodes and installed component types.
        var nodes = security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == request.FormVersionId)
            .Select(x => new { x.id, x.parent_id, x.key, x.label, x.description,
                x.node_type_id, x.configuration_json, x.position, x.min_occurs, x.max_occurs })
            .ToArray();
        var componentTypes = security.Query<FormNodeTypeModel>(SecurityOperations.Read)
            .OrderBy(x => x.renderer_key)
            .ToArray()
            .Where(x => FormComponentCatalogEndpoints.IsSupported(x.renderer_key))
            .ToArray();
        var typeById = componentTypes.ToDictionary(x => x.id);
        // Do not fabricate a partial form when RBAC hides some of its nodes.
        if (nodes.Any(x => !typeById.ContainsKey(x.node_type_id)))
            return Results.Problem(statusCode: 403, title: "Some form components are not accessible.");
        if (nodes.Length > 400)
            return Results.Problem(statusCode: 422, title: "Form exceeds the AI editor node limit.");
        // The roots have parent_id == null. Dictionary does not accept null keys,
        // even when its key type is Guid?. Keep roots separate from indexed children.
        var rootNodes = nodes.Where(x => x.parent_id is null)
            .OrderBy(x => x.position).ToArray();
        var childrenByParent = nodes.Where(x => x.parent_id.HasValue)
            .GroupBy(x => x.parent_id!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.position).ToArray());
        var visited = new HashSet<Guid>();
        JsonObject BuildChildren(Guid? parent, int depth)
        {
            if (depth > 20) throw new InvalidOperationException("Form hierarchy is too deep.");
            var result = new JsonObject();
            var children = rootNodes;
            if (parent.HasValue && !childrenByParent.TryGetValue(parent.Value, out children))
                return result;
            foreach (var node in children)
            {
                if (!visited.Add(node.id)) throw new InvalidOperationException("Form hierarchy has a cycle.");
                if (string.IsNullOrWhiteSpace(node.key) || result.ContainsKey(node.key))
                    throw new InvalidOperationException("Form hierarchy has missing or duplicate keys.");
                JsonNode configNode;
                try { configNode = JsonNode.Parse(node.configuration_json ?? "{}") ?? new JsonObject(); }
                catch (JsonException) { configNode = new JsonObject(); }
                if (configNode is not JsonObject) configNode = new JsonObject();
                result[node.key] = new JsonObject
                {
                    ["id"] = node.id.ToString("D"),
                    ["renderer_key"] = typeById[node.node_type_id].renderer_key,
                    ["label"] = node.label,
                    ["description"] = node.description,
                    ["position"] = node.position,
                    ["min_occurs"] = node.min_occurs,
                    ["max_occurs"] = node.max_occurs,
                    ["own_security_scope"] = false,
                    ["configuration"] = configNode,
                    ["children"] = BuildChildren(node.id, depth + 1)
                };
            }
            return result;
        }
        JsonObject tree;
        try
        {
            tree = BuildChildren(null, 0);
            if (visited.Count != nodes.Length)
                throw new InvalidOperationException("Form has orphaned or inaccessible parent nodes.");
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Cannot generate a complete form schema for AI agent");
            return Results.Problem(statusCode: 422, title: "Form hierarchy cannot be serialized as a tree.");
        }
        var componentCatalog = componentTypes.Select(x => new
        {
            x.id, x.renderer_key, x.name,
            description = FormComponentCatalogEndpoints.GetDescription(x.renderer_key),
            x.can_have_children, x.can_have_value, x.can_repeat,
            configuration_example = FormComponentCatalogEndpoints.GetExample(x.renderer_key)
        }).ToArray();
        var payload = new
        {
            contract = config["contract"] ?? "form-design.v1",
            request_id = Guid.NewGuid().ToString("D"),
            instruction = request.Instruction,
            context = new
            {
                form_version_id = request.FormVersionId,
                form_schema = new JsonObject
                {
                    ["form_version_id"] = request.FormVersionId.ToString("D"),
                    ["children"] = tree
                },
                component_catalog = componentCatalog
            }
        };

        var timeout = Math.Clamp(config.GetValue("timeout_seconds", 120), 1, 300);
        try
        {
            using var client = clients.CreateClient("digital-process-ai-agents");
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout));
            using var response = await client.PostAsJsonAsync(uri, payload, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                log.LogWarning("Form design agent error {StatusCode}", (int)response.StatusCode);
                return Results.Problem(statusCode: 502, title: "Form design agent returned an error.");
            }
            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            using var document = await JsonDocument.ParseAsync(stream,
                new JsonDocumentOptions { MaxDepth = 64 }, timeoutCts.Token);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
                return Results.Problem(statusCode: 502, title: "Invalid AI agent response contract.");
            // A proposal is not trusted data; validate the tree and RBAC again before any future save.
            return Results.Json(root.Clone());
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Results.Problem(statusCode: 504, title: "Form design agent timed out.");
        }
        catch (HttpRequestException ex)
        {
            log.LogWarning(ex, "Form design AI transport failed");
            return Results.Problem(statusCode: 502, title: "Form design agent is unavailable.");
        }
        catch (JsonException ex)
        {
            log.LogWarning(ex, "Invalid form design AI response");
            return Results.Problem(statusCode: 502, title: "Form design agent returned invalid JSON.");
        }
    }
    public sealed record ApproveRequest(Guid FormVersionId, JsonElement Schema);

    private static async Task<IResult> Approve(
        ApproveRequest request,
        ISecurityLayer security,
        IBulkTreeService bulk,
        CancellationToken ct)
    {
        var version = await security.GetByIdAsync<FormVersionModel>(request.FormVersionId, SecurityOperations.Update, ct);
        if (version is null) return Results.NotFound();
        if (version.is_published) return Results.BadRequest(new { error = "Published form versions are immutable. Create a new draft version first." });
        if (request.Schema.ValueKind != JsonValueKind.Object ||
            !request.Schema.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Object)
            return Results.BadRequest(new { error = "Expected a FormSchema object with children." });

        var old = security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == request.FormVersionId).ToArray();
        var types = security.Query<FormNodeTypeModel>(SecurityOperations.Read).ToArray();
        var byRenderer = types.GroupBy(x => x.renderer_key).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        var siblings = old.GroupBy(x => x.parent_id?.ToString("D") ?? "").ToDictionary(x => x.Key, x => x.ToDictionary(n => n.key, StringComparer.Ordinal));
        var count = 0;
        try
        {
            List<BulkFormTreeNode> Convert(JsonElement entries, Guid? parentId, int depth)
            {
                if (depth > 20) throw new ArgumentException("Form tree exceeds the maximum depth.");
                var output = new List<BulkFormTreeNode>();
                foreach (var property in entries.EnumerateObject())
                {
                    if (++count > 400) throw new ArgumentException("Form tree exceeds 400 nodes.");
                    var key = property.Name;
                    if (!Regex.IsMatch(key, "^[A-Za-z_][A-Za-z0-9_]*$"))
                        throw new ArgumentException($"Invalid FormNode key: {key}");
                    var node = property.Value;
                    if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty("renderer_key", out var kind))
                        throw new ArgumentException($"Missing renderer_key for {key}");
                    var renderer = kind.GetString() ?? "";
                    if (!byRenderer.TryGetValue(renderer, out var type))
                        throw new ArgumentException($"Unavailable FormNode renderer: {renderer}");
                    siblings.TryGetValue(parentId?.ToString("D") ?? "", out var existingSiblings);
                    var existing = existingSiblings is not null && existingSiblings.TryGetValue(key, out var candidate) ? candidate : null;
                    if (existing is not null && existing.node_type_id != type.id)
                        throw new ArgumentException($"Changing the renderer of existing node {key} is not supported.");
                    string? label = node.TryGetProperty("label", out var labelValue) && labelValue.ValueKind == JsonValueKind.String ? labelValue.GetString() : key;
                    string? description = node.TryGetProperty("description", out var descr) && descr.ValueKind == JsonValueKind.String ? descr.GetString() : null;
                    var config = node.TryGetProperty("configuration", out var configuration) && configuration.ValueKind == JsonValueKind.Object ? configuration.GetRawText() : "{}";
                    var childrenNodes = new List<BulkFormTreeNode>();
                    if (node.TryGetProperty("children", out var nested) && nested.ValueKind == JsonValueKind.Object)
                    {
                        if (nested.EnumerateObject().Any() && !type.can_have_children)
                            throw new ArgumentException($"Component {renderer} cannot have children.");
                        childrenNodes = Convert(nested, existing?.id, depth + 1);
                    }
                    output.Add(new BulkFormTreeNode(existing?.id, key, type.id, label, description,
                        output.Count, existing?.min_occurs ?? 1, existing?.max_occurs ?? 1, config, childrenNodes));
                }
                return output;
            }
            var rows = Convert(children, null, 0);
            var saved = await bulk.UpsertFormTreeAsync(request.FormVersionId, new BulkFormTreeRequest(rows), ct);
            return Results.Json(new { success = true, form_version_id = request.FormVersionId, node_count = count });
        }
        catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return Results.BadRequest(new { error = ex.Message }); }
        catch (JsonException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }

}
