namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
/// <summary>
/// Typ Form Backend Parameter Binding ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed record FormBackendParameterBinding(
    string parameter,
    FormBackendParameterSource source,
    string key);
