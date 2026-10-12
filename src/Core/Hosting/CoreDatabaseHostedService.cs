using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.Hosting;

/// <summary>
/// Owns Core database lifecycle: schema/bootstrap/security projection on startup
/// and JSON database dump on graceful shutdown.
/// </summary>
public sealed class CoreDatabaseHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CoreDatabaseHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);
        // For development/test databases EnsureCreated supplies the initial schema.
        var systemDataSource = configuration["SystemData:Source"]
            ?? Environment.GetEnvironmentVariable("SYSTEMDATA_SOURCE")
            ?? SystemDataImporter.DefaultSource;
        var importSystemData = configuration.GetValue("SystemData:Import", true);
        await DatabaseInitializer.InitializeAsync(db, systemDataSource, importSystemData, cancellationToken);

        var securityProjection = scope.ServiceProvider.GetRequiredService<ISecurityProjectionService>();
        var hasProjection = await db.EffectiveAccesses.AnyAsync(cancellationToken);
        var hasSecuritySources = await db.Roles.AnyAsync(cancellationToken)
            || await db.RbacAccessBindings.AnyAsync(cancellationToken);

        // Runtime mutations keep EffectiveAccess transactionally synchronized. A full
        // rebuild is therefore needed only when bootstrapping an empty projection
        // (fresh import/legacy database) or when explicitly requested for repair.
        var forceRebuild = configuration.GetValue("SecurityProjection:RebuildOnStartup", false);
        if (forceRebuild || (!hasProjection && hasSecuritySources))
        {
            logger.LogInformation("Rebuilding EffectiveAccess projection during startup bootstrap/repair.");
            await securityProjection.RebuildAllAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation("EffectiveAccess projection already present; full startup rebuild skipped.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
