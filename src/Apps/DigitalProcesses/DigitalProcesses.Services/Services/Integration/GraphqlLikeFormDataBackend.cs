using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed class GraphqlLikeFormDataBackend(HttpClient http, FormDataBackendOptions options, ILogger<GraphqlLikeFormDataBackend> logger, IHttpContextAccessor contextAccessor) : IFormDataBackend
{
    public Task<FormBackendPreparedRequest> PreprocessAsync(FormBackendOperationRequest request, FormValueSnapshot values, CancellationToken ct = default)
    {
        logger.LogInformation("GraphQL preflight start: expressionCount={ExpressionCount}, snapshotFields={FieldCount}", request.Variables?.Count ?? 0, values.Values.Count);
        var variables = FormVariableResolver.Resolve(request, values);
        logger.LogInformation("GraphQL preflight complete: variableNames={VariableNames}", string.Join(",", variables.Select(v => v.Key)));
        logger.LogDebug("GraphQL query: {Query}", request.query);
        logger.LogDebug("GraphQL resolved variables: {Variables}", JsonSerializer.Serialize(variables));
        return Task.FromResult(new FormBackendPreparedRequest(request.query, variables));
    }

    public async Task<FormBackendRawResponse> FetchAsync(FormBackendPreparedRequest request, CancellationToken ct = default)
    {
        var endpoint = options.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("DigitalProcesses:DataBackend:Endpoint is not configured.");

        logger.LogInformation("GraphQL transport request starting: endpoint={Endpoint}, query={Query}", endpoint, request.query);
        using var outgoing = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new { query = request.query, variables = request.variables })
        };
        FormBackendHeaderForwarder.Apply(contextAccessor.HttpContext?.Request, outgoing, options);
        logger.LogInformation("GraphQL forwarded header names: {Headers}", string.Join(",", outgoing.Headers.Select(x => x.Key)));
        using var response = await http.SendAsync(outgoing, ct);
        logger.LogInformation("GraphQL transport response: statusCode={StatusCode}", (int)response.StatusCode);
        var rawBody = await response.Content.ReadAsStringAsync(ct);
        // Full GraphQL payloads may contain personal data. Enable Trace only in a protected development environment.
        logger.LogTrace("GraphQL raw HTTP response: endpoint={Endpoint}, httpStatus={Status}, body={Body}", endpoint, (int)response.StatusCode, rawBody);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"GraphQL HTTP {(int)response.StatusCode}: {rawBody}");
        var payload = JsonNode.Parse(rawBody);
        logger.LogInformation("GraphQL transport payload: jsonKind={JsonKind}, topLevelFields={TopLevelFields}",
            payload is JsonObject ? "object" : payload is JsonArray ? "array" : payload is null ? "null" : "scalar",
            payload is JsonObject obj ? string.Join(",", obj.Select(entry => entry.Key)) : "(not an object)");
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
