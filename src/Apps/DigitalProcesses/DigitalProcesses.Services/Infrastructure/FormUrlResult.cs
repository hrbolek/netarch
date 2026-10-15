using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>
/// Typ Form Url Result ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed record FormUrlResult(string? Url, string? Error)
{
    public bool Success => Url is not null;
}
