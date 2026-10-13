using System.Text.Json;

namespace WebAppArchitecture.Events;

/// <summary>Bounded, read-only filter accepted by EntityLookupSelect (where: _ilike / _or).</summary>
/// <remarks>Unsupported operators are rejected rather than silently widening results.</remarks>
public static class EventLookupFilter
{
    public static List<T> Apply<T>(IEnumerable<T> visible, string? where, int? take, Func<T, string> label)
    {
        var result = visible;
        if (!string.IsNullOrWhiteSpace(where))
        {
            if (where.Length > 4096) return new();
            try
            {
                using var doc = JsonDocument.Parse(where);
                var patterns = new List<string>();
                if (!Extract(doc.RootElement, patterns) || patterns.Count == 0) return new();
                result = result.Where(x => patterns.Any(p => label(x).Contains(p, StringComparison.OrdinalIgnoreCase)));
            }
            catch (JsonException) { return new(); }
        }
        if (take.HasValue) result = result.Take(Math.Clamp(take.Value, 1, 100));
        return result.ToList();
    }

    private static bool Extract(JsonElement node, List<string> patterns)
    {
        if (patterns.Count > 16) return false;
        if (node.ValueKind != JsonValueKind.Object) return false;
        foreach (var prop in node.EnumerateObject())
        {
            if (prop.NameEquals("_or") && prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in prop.Value.EnumerateArray()) if (!Extract(element, patterns)) return false;
            }
            else if (prop.NameEquals("name") || prop.NameEquals("code"))
            {
                if (prop.Value.ValueKind != JsonValueKind.Object || !prop.Value.TryGetProperty("_ilike", out var test) || test.ValueKind != JsonValueKind.String) return false;
                var s = test.GetString()?.Trim('%', ' ', '\t');
                if (!string.IsNullOrWhiteSpace(s)) patterns.Add(s);
            }
            else return false;
        }
        return true;
    }
}
