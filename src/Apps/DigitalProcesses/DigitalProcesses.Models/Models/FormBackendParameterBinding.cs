using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Typ Form Backend Parameter Binding ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed record FormBackendParameterBinding(
    string parameter,
    FormBackendParameterSource source,
    string key);
