namespace WebAppArchitecture.DigitalProcesses.Models;

public enum FormBackendParameterSource
{
    FormValue,
    RuntimeValue
}

public sealed record FormBackendParameterBinding(
    string parameter,
    FormBackendParameterSource source,
    string key);

public sealed record FormBackendOperationRequest(
    string query,
    IReadOnlyCollection<FormBackendParameterBinding> parameters);
