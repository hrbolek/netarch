using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.FormNode;
using WebAppArchitecture.DigitalProcesses.Services.Submission;
using WebAppArchitecture.User.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebAppArchitecture.User.Services;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>
/// Development adapter backed by the same secured IUserService as the existing
/// /architecture/UserResponse/list endpoint. No separate HTTP endpoint or fixture is created.
/// </summary>
/// <summary>
/// Typ Dummy Jsonata Caller ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed class DummyJsonataCaller(IUserService users) : IFormDataBackend
{
    public Task<FormBackendPreparedRequest> PreprocessAsync(
        FormBackendOperationRequest request, FormValueSnapshot values, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var variables = FormVariableResolver.Resolve(request, values);
        return Task.FromResult(new FormBackendPreparedRequest(request.query, variables));
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public async Task<FormBackendRawResponse> FetchAsync(FormBackendPreparedRequest request, CancellationToken ct = default)
    {
        // Development-only caller: accept any GraphQL document, but always return
        // the users available through the existing secured service.
        // GetAllAsync uses the same secured service as the existing UserResponse endpoints.
        var records = await users.GetAllAsync<UserResponse>(ct);
        return new FormBackendRawResponse(JsonSerializer.SerializeToNode(records));
    }

    public Task<JsonNode?> PostprocessAsync(
        FormBackendOperationRequest request, FormBackendRawResponse response, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = response.payload?.DeepClone();
        if (result is JsonArray array)
        {
            foreach (var entry in array)
            {
                if (entry is not JsonObject user) continue;
                var id = user["id"]?.ToString();
                var name = user["Name"]?.ToString() ?? user["name"]?.ToString() ?? "";
                var surname = user["surname"]?.ToString() ?? "";
                user["value"] = id;
                user["label"] = $"{name} {surname}".Trim();
            }
        }
        return Task.FromResult(result);
    }

    public async Task<JsonNode?> ExecuteAsync(
        FormBackendOperationRequest request, FormValueSnapshot values, CancellationToken ct = default)
    {
        var prepared = await PreprocessAsync(request, values, ct);
        var raw = await FetchAsync(prepared, ct);
        return await PostprocessAsync(request, raw, ct);
    }
}
