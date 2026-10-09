using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

public sealed class UnconfiguredDigitalProcessIntegration : IDigitalProcessIntegration
{
    public Task<string?> ExecuteAsync(string query, string? inputJson, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("DigitalProcesses integration sidecar is not configured.");
}
