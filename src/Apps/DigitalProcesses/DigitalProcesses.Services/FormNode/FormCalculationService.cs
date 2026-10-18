using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNode;
/// <summary>
/// Shared read-only computation for form renderers. External evaluations use the
/// existing secured IFormDataBackend/JSONata contract; no direct outbound URL is accepted.
/// </summary>
/// <summary>
/// Služba I Form Calculation Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>

public interface IFormCalculationService
{
    Task<JsonNode?> EvaluateAsync(JsonObject configuration,
        IReadOnlyDictionary<string, JsonNode?> sectionValues,
        CancellationToken ct = default);
}
/// <summary>
/// Služba Form Calculation Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
/// <remarks>
/// Architecture exception for the calculation helper, not for production endpoint orchestration:
/// this service evaluates supplied configuration/values and owns no database entity. Its backend
/// transport is deployment-configured; callers must load configuration and values through secured
/// domain services before evaluation. It does not establish RBAC access to a form or submission.
/// Direct use is allowed in temporary designer previews; production resolvers should obtain
/// authorized domain results from entity services or auxiliary services composing them.
/// </remarks>

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
        // Explicit values take precedence; all keys are also available via $.form.
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

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
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
