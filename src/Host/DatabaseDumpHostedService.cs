using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Host;

/// <summary>
/// Writes the configured JSON database snapshot during graceful host shutdown.
/// ASP.NET Core awaits StopAsync for Ctrl+C/SIGTERM, so the dump is completed
/// before process termination whenever the host receives a graceful stop signal.
/// </summary>
public sealed class DatabaseDumpHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DatabaseDumpHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
