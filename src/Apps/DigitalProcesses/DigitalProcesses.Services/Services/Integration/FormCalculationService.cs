using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Shared read-only computation for form renderers. External evaluations use the
/// existing secured IFormDataBackend/JSONata contract; no direct outbound URL is accepted.
/// </summary>
public interface IFormCalculationService
{
    Task<JsonNode?> EvaluateAsync(JsonObject configuration,
        IReadOnlyDictionary<string, JsonNode?> sectionValues,
        CancellationToken ct = default);
}

public sealed class FormCalculationService(IFormDataBackend backend) : IFormCalculationService
{
    public async Task<JsonNode?> EvaluateAsync(JsonObject configuration,
        IReadOnlyDictionary<string, JsonNode?> sectionValues,
        CancellationToken ct = default)
    {
        var calculation = configuration["calculation"] as JsonObject
            ?? configuration["dataSource"] as JsonObject
            ?? throw new InvalidOperationException("Missing calculation/dataSource configuration.");
        var query = calculation["query"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(query))
            throw new InvalidOperationException("Calculation requires a GraphQL query.");

        var variables = calculation["variables"] as JsonObject;
        var arguments = variables?.DeepClone() as JsonObject ?? new JsonObject();
        // Previous select contract: section values are available as named query variables.
        // Explicit values take precedence, all keys are still available via $form.
        foreach (var (key, value) in sectionValues)
            if (!arguments.ContainsKey(key)) arguments[key] = value?.DeepClone();

        var request = new FormBackendOperationRequest(query,
            Array.Empty<FormBackendParameterBinding>(), arguments);
        var snapshot = new FormValueSnapshot(sectionValues,
            new Dictionary<string, JsonNode?>());
        var response = await backend.ExecuteAsync(request, snapshot, ct);
        var path = calculation["path"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(path)) return response?.DeepClone();
        return SelectPath(response, path)?.DeepClone();
    }

    public static JsonNode? SelectPath(JsonNode? value, string path)
    {
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (value is JsonObject obj) value = obj[part];
            else if (value is JsonArray arr && int.TryParse(part, out var index) && index >= 0 && index < arr.Count)
                value = arr[index];
            else return null;
        }
        return value;
    }
}
