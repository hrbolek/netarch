using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed class FormDataBackendOptions
{
    public string? Endpoint { get; set; }
    /// <summary>Admin-controlled allowlist; never supplied by form JSON.</summary>
    public string[] ForwardHeaders { get; set; } = Array.Empty<string>();
}
