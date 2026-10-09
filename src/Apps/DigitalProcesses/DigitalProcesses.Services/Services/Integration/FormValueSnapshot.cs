using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed record FormValueSnapshot(
    IReadOnlyDictionary<string, JsonNode?> Values,
    IReadOnlyDictionary<string, JsonNode?> RuntimeValues)
{
    public JsonNode? Resolve(FormBackendParameterBinding binding)
        => binding.source switch
        {
            FormBackendParameterSource.FormValue => ResolveFormValue(binding.key),
            FormBackendParameterSource.RuntimeValue => RuntimeValues.TryGetValue(binding.key, out var value) ? value?.DeepClone() : null,
            _ => null
        };

    private JsonNode? ResolveFormValue(string qualifiedKey)
    {
        if (Values.TryGetValue(qualifiedKey, out var exact))
            return exact?.DeepClone();

        var prefix = qualifiedKey.TrimEnd('.') + ".";
        var descendants = Values
            .Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal))
            .ToArray();
        if (descendants.Length == 0) return null;

        var root = new JsonObject();
        foreach (var pair in descendants)
            SetNested(root, pair.Key[prefix.Length..], pair.Value?.DeepClone());
        return root;
    }

    private static void SetNested(JsonObject root, string relativeKey, JsonNode? value)
    {
        var parts = relativeKey.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return;
        var current = root;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (current[parts[i]] is not JsonObject child)
            {
                child = new JsonObject();
                current[parts[i]] = child;
            }
            current = child;
        }
        current[parts[^1]] = value;
    }
}
