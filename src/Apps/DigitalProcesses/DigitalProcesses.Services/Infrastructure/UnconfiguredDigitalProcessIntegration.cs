using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>
/// Typ Unconfigured Digital Process Integration ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed class UnconfiguredDigitalProcessIntegration : IDigitalProcessIntegration
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public Task<string?> ExecuteAsync(string query, string? inputJson, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("DigitalProcesses integration sidecar is not configured.");
}
