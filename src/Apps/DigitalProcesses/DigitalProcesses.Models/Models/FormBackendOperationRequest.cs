using System.Text.Json.Nodes;
namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormBackendOperationRequest(
    string query,
    IReadOnlyCollection<FormBackendParameterBinding> parameters,
    JsonObject? Variables = null);
