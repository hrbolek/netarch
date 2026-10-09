namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormBackendParameterBinding(
    string parameter,
    FormBackendParameterSource source,
    string key);
