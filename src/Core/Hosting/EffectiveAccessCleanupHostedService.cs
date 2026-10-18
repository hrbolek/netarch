using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Hosting;

/// <summary>
/// Physically removes expired EffectiveAccess projection rows outside request hot paths.
/// Runtime authorization ignores them immediately via valid_to_unix_ms.
/// </summary>

public sealed class EffectiveAccessCleanupHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EffectiveAccessCleanupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Max(15, configuration.GetValue("SecurityProjection:CleanupIntervalMinutes", 360));
        var retentionHours = Math.Max(1, configuration.GetValue("SecurityProjection:RetentionHours", 24));

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<IDBLayer>();
                var cutoff = DateTimeOffset.UtcNow.AddHours(-retentionHours).ToUnixTimeMilliseconds();
                var removed = await db.DeleteWhereAsync<EffectiveAccessModel>(
                    x => x.valid_to_unix_ms != null && x.valid_to_unix_ms < cutoff,
                    stoppingToken);
                if (removed > 0)
                    logger.LogInformation("Removed {Count} expired EffectiveAccess projection rows.", removed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EffectiveAccess background cleanup failed.");
            }
        }
    }
}
