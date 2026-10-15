using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>
/// Typ Form Data Backend Options ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed class FormDataBackendOptions
{
    public string? Endpoint { get; set; }
    /// <summary>Admin-controlled allowlist; never supplied by form JSON.</summary>
    public string[] ForwardHeaders { get; set; } = Array.Empty<string>();
}
