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
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);

        var dumpPath = configuration["DatabaseDump:Path"]
            ?? Environment.GetEnvironmentVariable("DATABASE_DUMP_PATH")
            ?? "./data/dump.json";

        var dumpService = scope.ServiceProvider.GetRequiredService<IDatabaseDumpService>();
        var restoredFromDump = await dumpService.LoadIfExistsAsync(dumpPath, cancellationToken);

        if (restoredFromDump)
        {
            await DatabaseInitializer.InitializeAsync(
                db,
                systemDataSource: null,
                importSystemData: false);
        }
        else
        {
            var systemDataSource = configuration["SystemData:Source"]
                ?? Environment.GetEnvironmentVariable("SYSTEMDATA_SOURCE")
                ?? SystemDataImporter.DefaultSource;
            var importSystemData = configuration.GetValue("SystemData:Import", true);
            await DatabaseInitializer.InitializeAsync(db, systemDataSource, importSystemData);
        }

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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var path = configuration["DatabaseDump:Path"]
            ?? Environment.GetEnvironmentVariable("DATABASE_DUMP_PATH")
            ?? "./data/dump.json";

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dump = scope.ServiceProvider.GetRequiredService<IDatabaseDumpService>();
            await dump.SaveAsync(path, cancellationToken);
            logger.LogInformation("Database dump saved to {DumpPath}", path);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Database dump was cancelled during shutdown.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save database dump during shutdown.");
        }
    }
}
