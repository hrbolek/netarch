using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.Infrastructure;
/// <summary>
/// Typ I Digital Process Integration ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public interface IDigitalProcessIntegration
{
    Task<string?> ExecuteAsync(string query, string? inputJson, CancellationToken cancellationToken = default);
}

/// <summary>
/// Safe default. A real JSONata/GraphQL sidecar adapter should replace this registration.
/// Declared conditions/actions are never silently ignored.
/// </summary>
