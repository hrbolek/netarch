using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IDigitalProcessIntegration
{
    Task<string?> ExecuteAsync(string query, string? inputJson, CancellationToken cancellationToken = default);
}

/// <summary>
/// Safe default. A real JSONata/GraphQL sidecar adapter should replace this registration.
/// Declared conditions/actions are never silently ignored.
/// </summary>
public sealed class UnconfiguredDigitalProcessIntegration : IDigitalProcessIntegration
{
    public Task<string?> ExecuteAsync(string query, string? inputJson, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("DigitalProcesses integration sidecar is not configured.");
}
