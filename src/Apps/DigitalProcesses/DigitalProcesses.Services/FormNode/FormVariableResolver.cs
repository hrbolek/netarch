using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNode;
/// <summary>Evaluates form action variable expressions locally, before HTTP transport.</summary>
public static class FormVariableResolver
{
    public static JsonObject Resolve(FormBackendOperationRequest request, FormValueSnapshot snapshot)
    {
        var variables = new JsonObject();
        foreach (var binding in request.parameters)
            variables[binding.parameter] = snapshot.Resolve(binding);
        if (request.Variables is not null)
            foreach (var entry in request.Variables)
                variables[entry.Key] = Evaluate(entry.Value, snapshot);
        return variables;
    }

    private static JsonNode? Evaluate(JsonNode? input, FormValueSnapshot snapshot)
    {
        if (input is JsonObject obj)
        {
            var result = new JsonObject();
            foreach (var entry in obj)
                result[entry.Key] = Evaluate(entry.Value, snapshot);
            return result;
        }
        if (input is JsonArray array)
        {
            var result = new JsonArray();
            foreach (var entry in array)
                result.Add(Evaluate(entry, snapshot));
            return result;
        }
        if (input is JsonValue value && value.TryGetValue<string>(out var expression))
        {
            if (expression.StartsWith("$$", StringComparison.Ordinal))
                return JsonValue.Create(expression[1..]);
            if (expression is "$.form" or "$form") return BuildFormJson(snapshot.Values);
            if (expression.StartsWith("$.form.", StringComparison.Ordinal))
                return ResolveFormExpression(snapshot.Values, expression[7..], expression);
            if (expression.StartsWith("$form.", StringComparison.Ordinal))
                return ResolveFormExpression(snapshot.Values, expression[6..], expression);
            if (expression.StartsWith("$process.", StringComparison.Ordinal))
                return Require(snapshot.RuntimeValues, "process." + expression[9..], expression);
            if (expression.StartsWith("$context.", StringComparison.Ordinal))
                return Require(snapshot.RuntimeValues, "context." + expression[9..], expression);
            if (expression.StartsWith('$'))
                throw new InvalidOperationException($"Unsupported form variable expression '{expression}'.");
        }
        return input?.DeepClone();
    }

    private static JsonNode? ResolveFormExpression(IReadOnlyDictionary<string, JsonNode?> values, string key, string expression)
    {
        if (values.TryGetValue(key, out var exact)) return exact?.DeepClone();
        JsonNode? current = BuildFormJson(values);
        foreach (var part in System.Text.RegularExpressions.Regex.Matches(key, @"([^\.\[\]]+)|\[(\d+)\]"))
        {
            var match = (System.Text.RegularExpressions.Match)part;
            if (match.Groups[2].Success)
            {
                if (current is not JsonArray a || int.Parse(match.Groups[2].Value) >= a.Count)
                    throw new InvalidOperationException($"Unable to resolve '{expression}'.");
                current = a[int.Parse(match.Groups[2].Value)];
            }
            else
            {
                if (current is not JsonObject o || !o.TryGetPropertyValue(match.Groups[1].Value, out current))
                    throw new InvalidOperationException($"Unable to resolve '{expression}'.");
            }
        }
        return current?.DeepClone();
    }

    private static JsonObject BuildFormJson(IReadOnlyDictionary<string, JsonNode?> values)
    {
        var root = new JsonObject();
        foreach (var (path, node) in values)
        {
            JsonNode current = root;
            var segments = System.Text.RegularExpressions.Regex.Matches(path, @"([^\.\[\]]+)(?:\[(\d+)\])?");
            for (var n = 0; n < segments.Count; n++)
            {
                var segment = segments[n];
                var key = segment.Groups[1].Value;
                var indexed = segment.Groups[2].Success;
                var last = n == segments.Count - 1;
                if (current is not JsonObject obj) break;
                if (!indexed)
                {
                    if (last) obj[key] = node?.DeepClone();
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
                    var index = int.Parse(segment.Groups[2].Value);
                    if (index > 10000) throw new InvalidOperationException("Too many repeat instances.");
                    while (array.Count <= index) array.Add((JsonNode?)null);
                    if (last) array[index] = node?.DeepClone();
                    else
                    {
                        if (array[index] is not JsonObject) array[index] = new JsonObject();
                        current = array[index]!;
                    }
                }
            }
        }
        return root;
    }

    private static JsonNode? Require(IReadOnlyDictionary<string, JsonNode?> values, string key, string expression)
    {
        if (values.TryGetValue(key, out var exact)) return exact?.DeepClone();
        var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (var n = parts.Length - 1; n > 0; n--)
        {
            var prefix = string.Join('.', parts.Take(n));
            if (!values.TryGetValue(prefix, out var parent)) continue;
            JsonNode? node = parent;
            for (var i = n; i < parts.Length; i++)
                node = node is JsonObject obj ? obj[parts[i]] : null;
            return node?.DeepClone();
        }
        throw new InvalidOperationException($"Unable to resolve '{expression}': value '{key}' is missing.");
    }
}
