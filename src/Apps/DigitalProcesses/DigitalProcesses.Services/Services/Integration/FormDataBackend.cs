using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed class FormDataBackendOptions
{
    public string? Endpoint { get; set; }
}

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

public sealed record FormBackendPreparedRequest(string query, JsonObject variables);
public sealed record FormBackendRawResponse(JsonNode? payload);

public interface IFormDataBackend
{
    Task<FormBackendPreparedRequest> PreprocessAsync(FormBackendOperationRequest request, FormValueSnapshot values, CancellationToken ct = default);
    Task<FormBackendRawResponse> FetchAsync(FormBackendPreparedRequest request, CancellationToken ct = default);
    Task<JsonNode?> PostprocessAsync(FormBackendOperationRequest request, FormBackendRawResponse response, CancellationToken ct = default);
    Task<JsonNode?> ExecuteAsync(FormBackendOperationRequest request, FormValueSnapshot values, CancellationToken ct = default);
}

/// <summary>
/// One GraphQL-like endpoint is the only data-backend contract visible to Digital Processes.
/// Preprocess maps qualified form keys to query variables, Fetch performs transport, and
/// Postprocess is intentionally identity-by-default: response shape is enforced by the
/// operation contract rather than by per-form JSONata transformations.
/// </summary>
public sealed class GraphqlLikeFormDataBackend(HttpClient http, FormDataBackendOptions options) : IFormDataBackend
{
    public Task<FormBackendPreparedRequest> PreprocessAsync(FormBackendOperationRequest request, FormValueSnapshot values, CancellationToken ct = default)
    {
        var variables = new JsonObject();
        foreach (var binding in request.parameters)
            variables[binding.parameter] = values.Resolve(binding);
        return Task.FromResult(new FormBackendPreparedRequest(request.query, variables));
    }

    public async Task<FormBackendRawResponse> FetchAsync(FormBackendPreparedRequest request, CancellationToken ct = default)
    {
        var endpoint = options.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("DigitalProcesses:DataBackend:Endpoint is not configured.");

        using var response = await http.PostAsJsonAsync(endpoint, new { query = request.query, variables = request.variables }, ct);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
        return new FormBackendRawResponse(payload);
    }

    public Task<JsonNode?> PostprocessAsync(FormBackendOperationRequest request, FormBackendRawResponse response, CancellationToken ct = default)
        => Task.FromResult(response.payload?.DeepClone());

    public async Task<JsonNode?> ExecuteAsync(FormBackendOperationRequest request, FormValueSnapshot values, CancellationToken ct = default)
    {
        var prepared = await PreprocessAsync(request, values, ct);
        var raw = await FetchAsync(prepared, ct);
        return await PostprocessAsync(request, raw, ct);
    }
}
