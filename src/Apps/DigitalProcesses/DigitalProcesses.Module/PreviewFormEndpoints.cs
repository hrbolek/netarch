using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Services;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>Development-only preview endpoints. Values, queries and output are processed server-side.</summary>
internal static class PreviewFormEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/digital-processes/form-preview/data/{formVersionId:guid}/{previewInstanceId:guid}", async (
            Guid formVersionId, Guid previewInstanceId, IFormNodeService nodes,
            PreviewFormInstanceStore store, CancellationToken ct) =>
        {
            // GetDesignerAsync checks access to the source definition before revealing preview values.
            var designer = await nodes.GetDesignerAsync(formVersionId, ct);
            var flat = store.WithInstance(previewInstanceId, formVersionId,
                state => BuildQualifiedValues(state.Values, designer.nodes));
            return Results.Json(BuildStructuredJson(flat));
        }).WithTags("DigitalProcesses", "Preview")
          .WithName("PreviewFormJsonData")
          .WithDescription("Returns the JSON document of a temporary form preview, using the same reconstruction as export ($form). Preview state expires after inactivity.");

        endpoints.MapPost("/digital-processes/form-preview/values", async (
            HttpContext http, IFormNodeService nodeService, PreviewFormInstanceStore store, ILoggerFactory logs, CancellationToken ct) =>
        {
            var log = logs.CreateLogger("DigitalProcesses.FormPreview.Values");
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                var (instance, version) = Identity(form);
                log.LogInformation("Preview autosave received: instance={Instance}, version={Version}, fields={FieldCount}", instance, version, form.Count);
                var designer = await nodeService.GetDesignerAsync(version, ct);
                store.WithInstance(instance, version, state => { Capture(state, form, designer); return 0; });
                return Results.NoContent();
            }
            catch (Exception ex) when (ex is InvalidOperationException or FormatException or KeyNotFoundException)
            {
                log.LogWarning(ex, "Preview autosave failed");
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithTags("DigitalProcesses", "Preview")
          .WithDescription("Autosaves preview form values into temporary server-side state; never creates a Submission.");

        endpoints.MapPost("/digital-processes/form-preview/actions/{actionId:guid}", async (
            Guid actionId, HttpContext http, IFormNodeService nodeService,
            PreviewFormInstanceStore store, IFormDataBackend backend, ILoggerFactory logs, CancellationToken ct) =>
        {
            var log = logs.CreateLogger("DigitalProcesses.FormPreview.Import");
            try
            {
                var form = await http.Request.ReadFormAsync(ct);
                log.LogInformation("Import request received: action={ActionId}, submittedFields={FieldCount}, hasInstance={HasInstance}, hasVersion={HasVersion}",
                    actionId, form.Count, !string.IsNullOrWhiteSpace(form["previewInstanceId"].ToString()),
                    !string.IsNullOrWhiteSpace(form["formVersionId"].ToString()));
                var (instance, version) = Identity(form);
                log.LogInformation("Import identity resolved: instance={Instance}, version={Version}, action={ActionId}", instance, version, actionId);
                // GetDesignerAsync enforces caller's existing read access to the definition.
                var designer = await nodeService.GetDesignerAsync(version, ct);
                var action = designer.nodes.SingleOrDefault(n => n.id == actionId && n.form_version_id == version)
                    ?? throw new InvalidOperationException("Action is not part of this form version.");
                var type = designer.node_types.SingleOrDefault(t => t.id == action.node_type_id);
                var isExport = type?.renderer_key == "action.export";
                if (!isExport && type?.renderer_key != "action.import")
                    throw new InvalidOperationException("Requested node is neither an import nor an export action.");
                var configuration = JsonNode.Parse(action.configuration_json ?? "{}") as JsonObject
                    ?? throw new InvalidOperationException("Invalid action configuration.");
                var query = configuration["query"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(query))
                    throw new InvalidOperationException("Missing GraphQL query in saved action configuration.");
                var variables = configuration["variables"] as JsonObject;
                log.LogInformation("Import definition loaded: action={ActionId}, queryLength={QueryLength}, variableNames={VariableNames}",
                    actionId, query.Length, variables is null ? "(none)" : string.Join(",", variables.Select(x => x.Key)));
                // Capture and preflight use the same state; keep snapshot ownership server-side.
                var snapshot = store.WithInstance(instance, version, state =>
                {
                    Capture(state, form, designer);
                    return new FormValueSnapshot(BuildQualifiedValues(state.Values, designer.nodes),
                        new Dictionary<string, JsonNode?>());
                });
                log.LogInformation("Import preflight/transport starting: action={ActionId}, snapshotFieldCount={FieldCount}", actionId, snapshot.Values.Count);
                // Export has the same transport as import. The form JSON can be referenced by
                // $form.* bindings, or supplied as the default GraphQL variable "form".
                if (isExport)
                {
                    variables = variables?.DeepClone() as JsonObject ?? new JsonObject();
                    if (!variables.ContainsKey("form"))
                        variables["form"] = BuildStructuredJson(snapshot.Values);
                }
                var result = await backend.ExecuteAsync(
                    new FormBackendOperationRequest(query, Array.Empty<FormBackendParameterBinding>(), variables), snapshot, ct);
                log.LogInformation("Import backend finished: action={ActionId}", actionId);
                if (result is not JsonObject response)
                {
                    var kind = result is JsonArray ? "JSON array" : result is null ? "null" : "JSON scalar";
                    throw new InvalidOperationException($"GraphQL backend returned {kind}, expected an object with 'data'. Check DigitalProcesses:DataBackend:Provider (must be GraphQL), Endpoint and container environment overrides.");
                }
                log.LogInformation("Import GraphQL response received: action={ActionId}, topLevelFields={Fields}", actionId, string.Join(",", response.Select(x => x.Key)));
                log.LogTrace("Import GraphQL response JSON: action={ActionId}, payload={Payload}", actionId, response.ToJsonString());
                if (response["errors"] is JsonArray errors && errors.Count > 0)
                    throw new InvalidOperationException("GraphQL errors: " + errors.ToJsonString());
                if (isExport)
                {
                    log.LogInformation("Export completed: action={ActionId}, responseFields={Fields}", actionId, string.Join(",", response.Select(x => x.Key)));
                    return Results.Content(RenderExport(action.id, action.label ?? "Export dat", instance, version, response), "text/html; charset=utf-8");
                }
                if (response["data"] is not JsonObject data)
                    throw new InvalidOperationException("GraphQL response does not contain object 'data'. Top-level keys: " + string.Join(", ", response.Select(x => x.Key)) + ". Check the configured endpoint and returned response format.");
                var imported = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
                Flatten(data, "", imported);
                // Mapping is relative to the containing section of the import action.
                // For example, sibling section `whoami` and its field `hostname`
                // consume response data.whoami.hostname. Never treat the action itself
                // as a destination and never match unrelated sections elsewhere.
                var matching = new List<(FormNodeResponse Node, string Key, JsonNode? Value)>();
                foreach (var node in designer.nodes)
                {
                    if (node.id == action.id) continue;
                    var renderer = designer.node_types.FirstOrDefault(t => t.id == node.node_type_id)?.renderer_key;
                    if (renderer is not ("input.text" or "input.number" or "input.date" or "input.textarea")) continue;
                    var key = RelativeKey(node, action.parent_id, designer.nodes);
                    if (key is null) continue;
                    foreach (var pair in imported)
                    {
                        if (pair.Key == key || IsIndexedMatch(key, pair.Key, designer.nodes, action.parent_id))
                            matching.Add((node, pair.Key, pair.Value));
                    }
                }
                log.LogInformation("Import field matching: action={ActionId}, scopeParent={ParentId}, incomingKeys={IncomingKeys}, matchedKeys={MatchedKeys}",
                    actionId, action.parent_id, string.Join(",", imported.Keys), string.Join(",", matching.Select(m => m.Key)));
                store.WithInstance(instance, version, state =>
                {
                    foreach (var item in matching)
                        state.Values[StateKey(item.Node, item.Key, action.parent_id, designer.nodes)] = item.Value?.DeepClone();
                    state.Revision++;
                    return 0;
                });
                log.LogInformation("Import saved: action={ActionId}, returnedFields={ReturnedFields}, mappedFields={MappedFields}", actionId, imported.Count, matching.Count);
                return Results.Content(Render(action.id, action.label ?? "Import dat", imported, matching, designer, instance, version, response, action.parent_id), "text/html; charset=utf-8");
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or
                System.Text.Json.JsonException or TaskCanceledException or FormatException or KeyNotFoundException)
            {
                log.LogError(ex, "Preview import failed: action={ActionId}", actionId);
                return Results.Content(RenderError(ex.Message, actionId, http.Request.HasFormContentType ? http.Request.Form : null), "text/html; charset=utf-8", statusCode: 200);
            }
        }).WithTags("DigitalProcesses", "Preview")
          .WithDescription("Loads the persisted import action, captures current form fields, resolves variables on the server, executes the configured backend and returns a replacement HTML fragment.");
    }

    private static (Guid Instance, Guid Version) Identity(IFormCollection form)
    {
        if (!Guid.TryParse(form["previewInstanceId"].ToString(), out var instance) || instance == Guid.Empty ||
            !Guid.TryParse(form["formVersionId"].ToString(), out var version) || version == Guid.Empty)
            throw new InvalidOperationException($"Missing or invalid preview identity: previewInstanceId={(string.IsNullOrWhiteSpace(form["previewInstanceId"].ToString()) ? "missing" : "invalid")}, formVersionId={(string.IsNullOrWhiteSpace(form["formVersionId"].ToString()) ? "missing" : "invalid")}. Request has no preview identity. Check browser Network > Payload and the rendered HTML of #dp-preview-form; reload Test view.");
        return (instance, version);
    }

    private static void Capture(PreviewFormInstance state, IFormCollection submitted, FormDesignerResponse designer)
    {
        foreach (var node in designer.nodes)
        {
            if (node.node_type_id == DigitalProcessWellKnown.FormNodeTypes.Import || node.node_type_id == DigitalProcessWellKnown.FormNodeTypes.Export) continue;
            // Values are constrained to configured node keys; arbitrary request keys are never persisted.
            var baseKey = "node-" + node.id.ToString("N");
            var fieldType = designer.node_types.FirstOrDefault(t => t.id == node.node_type_id)?.renderer_key;
            foreach (var key in submitted.Keys.Where(k => k == baseKey || k.StartsWith(baseKey + "__", StringComparison.Ordinal)))
            {
                var text = submitted[key].ToString();
                if (fieldType == "input.number" && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    state.Values[key[5..]] = JsonValue.Create(number);
                else if (fieldType == "input.checkbox")
                    state.Values[key[5..]] = JsonValue.Create(text is "on" or "true" or "True");
                else
                    state.Values[key[5..]] = JsonValue.Create(text);
            }
        }
        state.Revision++;
    }

    // Values are persisted by stable node ID; qualified JSON paths are constructed from
    // the hierarchy only at the integration boundary, so employee.id and approver.id coexist.
    private static Dictionary<string, JsonNode?> BuildQualifiedValues(
        IReadOnlyDictionary<string, JsonNode?> values, IReadOnlyCollection<FormNodeResponse> nodes)
    {
        var result = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            var path = RelativeKey(node, null, nodes);
            if (path is null) continue;
            foreach (var (stateKey, value) in values)
            {
                var prefix = node.id.ToString("N");
                if (stateKey == prefix) result[path] = value?.DeepClone();
                else if (stateKey.StartsWith(prefix + "__", StringComparison.Ordinal))
                    result[InsertIndices(path, stateKey[(prefix.Length + 2)..], node, nodes)] = value?.DeepClone();
            }
        }
        return result;
    }

    private static void Flatten(JsonObject obj, string prefix, Dictionary<string, JsonNode?> output)
    {
        foreach (var item in obj)
        {
            var path = string.IsNullOrEmpty(prefix) ? item.Key : prefix + "." + item.Key;
            if (item.Value is JsonObject child) Flatten(child, path, output);
            else if (item.Value is JsonArray array)
            {
                for (var i = 0; i < array.Count; i++)
                {
                    var indexed = path + "[" + i + "]";
                    if (array[i] is JsonObject nested) Flatten(nested, indexed, output);
                    else if (array[i] is JsonArray nestedArray)
                    {
                        var wrapper = new JsonObject { ["items"] = nestedArray.DeepClone() };
                        var intermediate = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
                        Flatten(wrapper, indexed, intermediate);
                        foreach (var v in intermediate) output[v.Key.Replace(indexed + ".items", indexed)] = v.Value;
                    }
                    else output[indexed] = array[i]?.DeepClone();
                }
            }
            else output[path] = item.Value?.DeepClone();
        }
    }

    // Returns the path of a field beneath the action's containing section.
    // A null scopeParent means that both action and destination are at form root.
    // Return null for nodes outside that subtree.
    private static string? RelativeKey(FormNodeResponse node, Guid? scopeParent, IReadOnlyCollection<FormNodeResponse> nodes)
    {
        var keys = new Stack<string>();
        var current = node;
        var seen = new HashSet<Guid>();
        while (true)
        {
            if (!seen.Add(current.id)) return null;
            if (current.id == scopeParent) return string.Join('.', keys);
            keys.Push(current.key);
            if (current.parent_id == scopeParent) return string.Join('.', keys);
            if (current.parent_id is not Guid parentId) return scopeParent is null ? string.Join('.', keys) : null;
            var parent = nodes.FirstOrDefault(n => n.id == parentId);
            if (parent is null) return null;
            current = parent;
        }
    }

    private static string StateKey(FormNodeResponse node, string incoming, Guid? scopeParent, IReadOnlyCollection<FormNodeResponse> nodes)
    {
        var template = RelativeKey(node, scopeParent, nodes) ?? node.key;
        var suffix = new List<string>();
        foreach (Match match in Regex.Matches(incoming, @"\[(\d+)\]")) suffix.Add(match.Groups[1].Value);
        return node.id.ToString("N") + (suffix.Count > 0 ? "__" + string.Join("_", suffix) : "");
    }

    private static bool IsIndexedMatch(string template, string incoming, IReadOnlyCollection<FormNodeResponse> nodes, Guid? scopeParent)
    {
        var t = template.Split('.');
        var i = incoming.Split('.');
        if (t.Length != i.Length) return false;
        for (var n = 0; n < t.Length; n++)
        {
            var clean = Regex.Replace(i[n], @"\[\d+\]", "");
            if (clean != t[n]) return false;
            if (clean != i[n])
            {
                // The definition must mark this segment as repeatable.
                var currentPath = string.Join('.', t.Take(n + 1));
                if (!nodes.Any(x => x.max_occurs > 1 && RelativeKey(x, scopeParent, nodes) == currentPath &&
                    Regex.Matches(i[n], @"\[(\d+)\]").All(m => int.Parse(m.Groups[1].Value) < x.max_occurs))) return false;
            }
        }
        return true;
    }

    private static string InsertIndices(string path, string indices, FormNodeResponse node, IReadOnlyCollection<FormNodeResponse> nodes)
    {
        var segments = path.Split('.');
        var ids = indices.Split('_');
        var cursor = 0;
        for (var i = 0; i < segments.Length && cursor < ids.Length; i++)
        {
            var partial = string.Join('.', segments.Take(i + 1));
            if (nodes.Any(x => x.max_occurs > 1 && RelativeKey(x, null, nodes) == partial))
                segments[i] += "[" + ids[cursor++] + "]";
        }
        return string.Join('.', segments);
    }

    private static JsonObject BuildStructuredJson(IReadOnlyDictionary<string, JsonNode?> flat)
    {
        var root = new JsonObject();
        foreach (var (path, value) in flat) SetJsonPath(root, path, value?.DeepClone());
        return root;
    }

    private static void SetJsonPath(JsonObject root, string path, JsonNode? value)
    {
        JsonNode current = root;
        var parts = Regex.Matches(path, @"([^\.\[\]]+)(?:\[(\d+)\])?");
        for (var n = 0; n < parts.Count; n++)
        {
            var part = parts[n];
            var key = part.Groups[1].Value;
            var indexed = part.Groups[2].Success;
            var last = n == parts.Count - 1;
            if (current is not JsonObject obj) return;
            if (!indexed)
            {
                if (last) obj[key] = value;
                else
                {
                    if (obj[key] is not JsonObject) obj[key] = new JsonObject();
                    current = obj[key]!;
                }
            }
            else
            {
                if (obj[key] is not JsonArray) obj[key] = new JsonArray();
                var array = (JsonArray)obj[key]!;
                var index = int.Parse(part.Groups[2].Value);
                if (index > 10000) throw new InvalidOperationException("Too many repeat instances.");
                while (array.Count <= index) array.Add((JsonNode?)null);
                if (last) array[index] = value;
                else
                {
                    if (array[index] is not JsonObject) array[index] = new JsonObject();
                    current = array[index]!;
                }
            }
        }
    }

    private static bool IsAncestor(Guid ancestor, FormNodeResponse node, IReadOnlyCollection<FormNodeResponse> nodes)
    {
        var visited = new HashSet<Guid>();
        var id = node.parent_id;
        while (id is Guid current && visited.Add(current))
        {
            if (current == ancestor) return true;
            id = nodes.FirstOrDefault(n => n.id == current)?.parent_id;
        }
        return false;
    }

    private static string RenderRepeatBody(FormNodeResponse section,
        IReadOnlyCollection<(FormNodeResponse Node, string Key, JsonNode? Value)> matched,
        FormDesignerResponse designer, Guid? scopeParent)
    {
        var path = RelativeKey(section, scopeParent, designer.nodes)!;
        var indexes = matched.Select(m => m.Key)
            .Where(x => x.StartsWith(path + "[", StringComparison.Ordinal))
            .Select(x => Regex.Match(x[path.Length..], @"^\[(\d+)\]"))
            .Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value))
            .Distinct().Order().Take(section.max_occurs).ToArray();
        var html = new StringBuilder();
        html.Append("<div id=\"repeat-body-").Append(section.id.ToString("N"))
            .Append("\" class=\"card-body\" data-repeat-body hx-swap-oob=\"outerHTML\">");
        if (indexes.Length == 0) indexes = [0];
        foreach (var index in indexes)
        {
            html.Append("<div data-repeat-instance data-occurrence=\"").Append(index).Append("\">");
            foreach (var node in designer.nodes.Where(n => n.parent_id == section.id).OrderBy(n => n.position))
                AppendRepeatingChild(html, node, matched, designer, scopeParent, path + "[" + index + "]");
            html.Append("</div>");
        }
        if (section.max_occurs > 1)
        {
            html.Append("<div class=\"d-flex gap-2 mt-2\" data-repeat-controls>")
                .Append("<button type=\"button\" class=\"btn btn-sm btn-outline-primary\" onclick=\"digitalProcessesAddOccurrence(this)\"")
                .Append(indexes.Length >= section.max_occurs ? " disabled" : "")
                .Append(">Add occurrence</button><span class=\"small text-muted\">Maximum ")
                .Append(section.max_occurs).Append("</span></div>");
        }
        return html.Append("</div>").ToString();
    }

    private static void AppendRepeatingChild(StringBuilder html, FormNodeResponse node,
        IReadOnlyCollection<(FormNodeResponse Node, string Key, JsonNode? Value)> matched,
        FormDesignerResponse designer, Guid? scopeParent, string parentPath)
    {
        var type = designer.node_types.FirstOrDefault(t => t.id == node.node_type_id)?.renderer_key;
        var path = parentPath + "." + node.key;
        if (type == "section")
        {
            html.Append("<section class=\"card mb-3\" data-form-node-id=\"").Append(node.id)
                .Append("\" data-max-occurs=\"").Append(node.max_occurs).Append("\" data-repeat-section=\"")
                .Append(node.max_occurs > 1 ? "true" : "false").Append("\">")
                .Append("<div class=\"card-header\"><strong>")
                .Append(WebUtility.HtmlEncode(node.label ?? node.key)).Append("</strong></div>")
                .Append("<div class=\"card-body\" id=\"repeat-body-").Append(node.id.ToString("N"))
                .Append("\" data-repeat-body>");
            var nested = matched.Where(m => m.Key.StartsWith(path + "[", StringComparison.Ordinal)).Select(m =>
                Regex.Match(m.Key[path.Length..], @"^\[(\d+)\]"))
                .Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value)).Distinct().Order().Take(node.max_occurs).ToArray();
            if (node.max_occurs == 1) nested = [0];
            if (nested.Length == 0) nested = [0];
            foreach (var idx in nested)
            {
                html.Append("<div data-repeat-instance data-occurrence=\"").Append(idx).Append("\">");
                var childBase = node.max_occurs > 1 ? path + "[" + idx + "]" : path;
                foreach (var child in designer.nodes.Where(x => x.parent_id == node.id).OrderBy(x => x.position))
                    AppendRepeatingChild(html, child, matched, designer, scopeParent, childBase);
                html.Append("</div>");
            }
            if (node.max_occurs > 1)
                html.Append("<div data-repeat-controls><button type=\"button\" class=\"btn btn-sm btn-outline-primary\" onclick=\"digitalProcessesAddOccurrence(this)\">Add occurrence</button></div>");
            html.Append("</div></section>");
            return;
        }
        if (type is not ("input.text" or "input.number" or "input.date" or "input.textarea")) return;
        var match = matched.FirstOrDefault(m => m.Node.id == node.id && m.Key == path);
        var suffix = string.Join("_", Regex.Matches(path, @"\[(\d+)\]").Select(m => m.Groups[1].Value));
        var name = "node-" + node.id.ToString("N") + (suffix.Length > 0 ? "__" + suffix : "");
        var value = WebUtility.HtmlEncode(AsText(match.Value));
        html.Append("<div class=\"mb-3\" data-form-node-id=\"").Append(node.id).Append("\">")
            .Append("<label class=\"form-label\" for=\"").Append(name).Append("\">")
            .Append(WebUtility.HtmlEncode(node.label ?? node.key)).Append("</label>");
        if (type == "input.textarea")
            html.Append("<textarea class=\"form-control\" rows=\"4\" name=\"").Append(name)
                .Append("\" id=\"").Append(name).Append("\">").Append(value).Append("</textarea>");
        else
            html.Append("<input class=\"form-control\" name=\"").Append(name).Append("\" id=\"").Append(name)
                .Append("\" type=\"").Append(type == "input.number" ? "number" : type == "input.date" ? "date" : "text")
                .Append("\" value=\"").Append(value).Append("\" />");
        html.Append("</div>");
    }

    private static string RenderExport(Guid id, string label, Guid instance, Guid version, JsonObject response)
    {
        var html = new StringBuilder(RenderActionButton(id, label, instance, version));
        html.Append("<div class=\"alert alert-success alert-dismissible fade show mt-2\" role=\"alert\">Export dokončen.")
            .Append("<button type=\"button\" class=\"btn-close\" data-bs-dismiss=\"alert\" aria-label=\"Close\"></button></div>");
        html.Append("<details class=\"small mt-2\"><summary>GraphQL odpověď</summary><pre>")
            .Append(WebUtility.HtmlEncode(response.ToJsonString()))
            .Append("</pre></details></div>");
        return html.ToString();
    }

    private static string Render(Guid actionId, string label, IReadOnlyDictionary<string, JsonNode?> values,
        IEnumerable<(FormNodeResponse Node, string Key, JsonNode? Value)> matching, FormDesignerResponse designer, Guid instanceId, Guid versionId, JsonObject response, Guid? scopeParent)
    {
        var matched = matching.ToArray();
        var html = new StringBuilder();
        html.Append(RenderActionButton(actionId, label, instanceId, versionId));
        html.Append("<span class=\"small text-success ms-2\">Import dokončen: ")
            .Append(matched.Length).Append(" polí aktualizováno.</span>");
        if (matched.Length == 0)
            html.Append("<div class=\"small text-warning\">Nenalezena odpovídající pole v sekci tlačítka. Zkontrolujte key sekcí a polí.</div>");
        // Inspectable by the authorized designer/test user without hunting through server logs.
        // The response is HTML-encoded and is never inserted as executable markup.
        html.Append("<details class=\"small mt-2\"><summary>GraphQL diagnostika (odpověď, data a mapování)</summary>")
            .Append("<div>Mapováno: ").Append(matched.Length).Append(" / ").Append(values.Count).Append(" polí</div>")
            .Append("<pre class=\"border rounded bg-light p-2 mt-2 overflow-auto\">")
            .Append(WebUtility.HtmlEncode(response.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true })))
            .Append("</pre></details>");
        html.Append("</div>");

        // Repeating containers are replaced atomically, retaining the immutable definition.
        var repeats = designer.nodes.Where(x => x.max_occurs > 1 &&
            RelativeKey(x, scopeParent, designer.nodes) is string path &&
            matched.Any(m => m.Key.StartsWith(path + "[", StringComparison.Ordinal))).ToArray();
        foreach (var section in repeats.Where(x => !repeats.Any(p => p.id != x.id && IsAncestor(p.id, x, designer.nodes))))
            html.Append(RenderRepeatBody(section, matched, designer, scopeParent));
        // Plain fields remain individually replaceable OOB elements.
        foreach (var item in matched.Where(m => !repeats.Any(section => IsAncestor(section.id, m.Node, designer.nodes))))
        {
            var renderer = designer.node_types.FirstOrDefault(t => t.id == item.Node.node_type_id)?.renderer_key;
            var wrapperId = "form-field-" + item.Node.id.ToString("N");
            var inputId = "form-node-" + item.Node.id.ToString("N");
            var name = "node-" + item.Node.id.ToString("N");
            var labelText = WebUtility.HtmlEncode(item.Node.label ?? item.Node.key);
            var value = WebUtility.HtmlEncode(AsText(item.Value));
            html.Append("<div id=\"").Append(wrapperId).Append("\" class=\"mb-3\" data-form-node-id=\"")
                .Append(item.Node.id).Append("\" hx-swap-oob=\"outerHTML\">")
                .Append("<label class=\"form-label\" for=\"").Append(inputId).Append("\">")
                .Append(labelText).Append("</label>");
            if (renderer == "input.textarea")
            {
                html.Append("<textarea id=\"").Append(inputId).Append("\" name=\"").Append(name)
                    .Append("\" class=\"form-control\" rows=\"4\">").Append(value).Append("</textarea>");
            }
            else
            {
                var inputType = renderer == "input.number" ? "number" : renderer == "input.date" ? "date" : "text";
                html.Append("<input id=\"").Append(inputId).Append("\" name=\"").Append(name)
                    .Append("\" class=\"form-control\" type=\"").Append(inputType)
                    .Append("\" value=\"").Append(value).Append("\" />");
            }
            if (!string.IsNullOrWhiteSpace(item.Node.description))
                html.Append("<div class=\"form-text\">").Append(WebUtility.HtmlEncode(item.Node.description)).Append("</div>");
            html.Append("</div>");
        }
        return html.ToString();
    }

    private static string RenderActionButton(Guid actionId, string label, Guid instanceId, Guid versionId) =>
        "<div class=\"mb-3\" data-external-data-import=\"true\" data-form-node-id=\"" + actionId +
        "\"><button type=\"button\" class=\"btn " + (label.Contains("Export", StringComparison.OrdinalIgnoreCase) ? "btn-outline-success" : "btn-outline-primary") + "\" hx-post=\"/digital-processes/form-preview/actions/" + actionId +
        "\" hx-include=\"#dp-preview-form\" hx-vals=\"{&quot;previewInstanceId&quot;:&quot;" + instanceId +
        "&quot;,&quot;formVersionId&quot;:&quot;" + versionId +
        "&quot;}\" hx-target=\"closest [data-external-data-import]\" hx-swap=\"outerHTML\">" +
        WebUtility.HtmlEncode(label) + "</button>";

    private static string AsText(JsonNode? value) => value is JsonValue v && v.TryGetValue<string>(out var s)
        ? s : value?.ToJsonString() ?? string.Empty;

    private static string RenderError(string error, Guid actionId, IFormCollection? form)
    {
        var instanceText = form?["previewInstanceId"].ToString();
        var versionText = form?["formVersionId"].ToString();
        var instance = Guid.TryParse(instanceText, out var i) ? i : Guid.Empty;
        var version = Guid.TryParse(versionText, out var v) ? v : Guid.Empty;
        return RenderActionButton(actionId, "Zkusit znovu", instance, version) +
            "<div class=\"alert alert-warning alert-dismissible fade show mt-2\" role=\"alert\">Akce selhala: " + WebUtility.HtmlEncode(error) +
            "<button type=\"button\" class=\"btn-close\" data-bs-dismiss=\"alert\" aria-label=\"Close\"></button></div></div>";
    }
}
