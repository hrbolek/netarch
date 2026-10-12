using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Typ I Form Data Backend ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
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
