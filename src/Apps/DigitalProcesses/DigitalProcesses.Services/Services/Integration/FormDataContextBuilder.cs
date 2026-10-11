using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>Public, already authorized data available to local form expressions.
/// Callers must filter field values and history using the effective read policy before building.</summary>
public sealed record FormContextMetadata(Guid? RequestId = null, Guid? SubmissionId = null,
    Guid? WorkflowVersionId = null, Guid? CurrentStateId = null, string? CurrentStateName = null);

/// <summary>One immutable transition: StateId is the state BEFORE transition, and
/// ArchivedSubmissionId is the snapshot of that state. StateName and ActorEmail
/// are resolved display properties, not additional stored historical identifiers.</summary>
public sealed record FormContextHistoryEntry(Guid StateId, string? StateName,
    Guid ActorUserId, string? ActorEmail, string? PublicNote,
    DateTimeOffset ChangedAt, Guid ArchivedSubmissionId);

public interface IFormDataContextBuilder
{
    JsonObject Build(IReadOnlyCollection<FormNodeResponse> nodes,
        IReadOnlyDictionary<string, JsonNode?> visibleValues,
        FormContextMetadata? metadata = null,
        IEnumerable<FormContextHistoryEntry>? visibleHistory = null);
}

public sealed class FormDataContextBuilder : IFormDataContextBuilder
{
    public JsonObject Build(IReadOnlyCollection<FormNodeResponse> nodes,
        IReadOnlyDictionary<string, JsonNode?> visibleValues,
        FormContextMetadata? metadata = null,
        IEnumerable<FormContextHistoryEntry>? visibleHistory = null)
    {
        metadata ??= new FormContextMetadata();
        var form = new JsonObject();
        var lookup = nodes.GroupBy(x => x.parent_id)
            .ToDictionary(x => x.Key, x => x.OrderBy(n => n.position).ThenBy(n => n.key).ToArray());
        var traversed = new HashSet<Guid>();
        Fill(form, null, lookup, visibleValues, traversed);
        var history = new JsonArray();
        foreach (var item in (visibleHistory ?? []).OrderBy(x => x.ChangedAt).ThenBy(x => x.ArchivedSubmissionId))
        {
            history.Add(new JsonObject
            {
                ["state"] = new JsonObject { ["id"] = item.StateId.ToString(), ["name"] = item.StateName },
                ["who"] = new JsonObject { ["id"] = item.ActorUserId.ToString(), ["email"] = item.ActorEmail },
                ["note"] = item.PublicNote,
                ["when"] = item.ChangedAt.ToUniversalTime().ToString("O"),
                ["submission_id"] = item.ArchivedSubmissionId.ToString()
            });
        }
        return new JsonObject
        {
            ["form"] = form,
            ["request"] = new JsonObject { ["id"] = metadata.RequestId?.ToString() },
            ["submission"] = new JsonObject { ["id"] = metadata.SubmissionId?.ToString() },
            ["workflow"] = new JsonObject
            {
                ["id"] = metadata.WorkflowVersionId?.ToString(),
                ["state"] = new JsonObject
                {
                    ["id"] = metadata.CurrentStateId?.ToString(),
                    ["name"] = metadata.CurrentStateName
                }
            },
            ["history"] = history
        };
    }

    private static void Fill(JsonObject output, Guid? parentId,
        Dictionary<Guid?, FormNodeResponse[]> nodes,
        IReadOnlyDictionary<string, JsonNode?> visibleValues, HashSet<Guid> traversed)
    {
        if (!nodes.TryGetValue(parentId, out var children)) return;
        foreach (var child in children)
        {
            if (!traversed.Add(child.id)) throw new InvalidOperationException("Cycle in FormNodes.");
            if (!Regex.IsMatch(child.key, "^[A-Za-z_][A-Za-z0-9_]*$"))
                throw new InvalidOperationException($"Invalid FormNode key '{child.key}'.");
            if (output.ContainsKey(child.key)) throw new InvalidOperationException($"Duplicate FormNode key '{child.key}'.");
            if (nodes.ContainsKey(child.id))
            {
                var section = new JsonObject();
                Fill(section, child.id, nodes, visibleValues, traversed);
                output[child.key] = section;
            }
            else if (visibleValues.TryGetValue(child.id.ToString(), out var value)
                  || visibleValues.TryGetValue(child.id.ToString("N"), out value)
                  || visibleValues.TryGetValue(child.path, out value)
                  || visibleValues.TryGetValue(child.key, out value))
            {
                output[child.key] = value?.DeepClone();
            }
            // Missing/denied values are deliberately absent, not null.
        }
    }
}

public interface IFormExpressionEvaluator
{
    JsonNode? Evaluate(JsonObject context, string expression);
}

/// <summary>Pure read-only $-rooted JSON property/index resolution; no code execution or remote requests.</summary>
public sealed class FormExpressionEvaluator : IFormExpressionEvaluator
{
    private static readonly Regex Token = new(@"\G(?:\.([A-Za-z_][A-Za-z0-9_]*)|\[(\d+)\])", RegexOptions.Compiled);
    public JsonNode? Evaluate(JsonObject context, string expression)
    {
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > 512 || expression[0] != '$')
            throw new FormatException("Expression must start with '$'.");
        JsonNode? current = context;
        var offset = 1;
        while (offset < expression.Length)
        {
            var match = Token.Match(expression, offset);
            if (!match.Success) throw new FormatException($"Invalid JSON path: {expression}");
            if (match.Groups[1].Success)
                current = current is JsonObject obj ? obj[match.Groups[1].Value] : null;
            else
            {
                if (!int.TryParse(match.Groups[2].Value, out var index)) return null;
                current = current is JsonArray array && index < array.Count ? array[index] : null;
            }
            offset += match.Length;
        }
        return current?.DeepClone();
    }
}

public sealed record FormUrlResult(string? Url, string? Error)
{
    public bool Success => Url is not null;
}

public interface IFormUrlTemplateRenderer
{
    FormUrlResult Render(string template, JsonObject context);
}

/// <summary>Replaces {$....} expressions with escaped URI component values. A missing
/// reference fails closed; only absolute HTTP(S) URLs are supported.</summary>
public sealed class FormUrlTemplateRenderer(IFormExpressionEvaluator evaluator) : IFormUrlTemplateRenderer
{
    private static readonly Regex Placeholder = new(@"\{(\$[^{}]+)\}", RegexOptions.Compiled);
    public FormUrlResult Render(string template, JsonObject context)
    {
        if (string.IsNullOrWhiteSpace(template)) return new(null, "URL template is empty.");
        if (template.Length > 4096) return new(null, "URL template exceeds 4096 characters.");
        if (Placeholder.Matches(template).Count > 32) return new(null, "Too many URL placeholders.");
        string? error = null;
        string replaced;
        try
        {
            replaced = Placeholder.Replace(template, match =>
            {
                var value = evaluator.Evaluate(context, match.Groups[1].Value);
                if (value is not JsonValue scalar || scalar.ToString().Length == 0)
                {
                    error = $"Missing value for {match.Groups[1].Value}.";
                    return string.Empty;
                }
                return Uri.EscapeDataString(scalar.ToString());
            });
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException or OverflowException or System.Text.Json.JsonException)
        { return new(null, "Invalid expression or value in URL template."); }
        if (error is not null) return new(null, error);
        if (replaced.Contains("{$", StringComparison.Ordinal)) return new(null, "Invalid URL placeholder.");
        if (replaced.Length > 8192) return new(null, "Computed URL is too long.");
        if (!Uri.TryCreate(replaced, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(url.UserInfo))
            return new(null, "Only absolute HTTP(S) URLs without credentials are allowed.");
        return new(url.AbsoluteUri, null);
    }
}
