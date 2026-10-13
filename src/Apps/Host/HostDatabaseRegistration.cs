using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Host;

/// <summary>
/// The executable host is the EF composition root. All Core and app services
/// resolve the *same scoped instance*, preserving one change tracker/transaction.
/// Call after AddCore() and all Add...() application registrations.
/// </summary>
public static class HostDatabaseRegistration
{
    public static IServiceCollection AddHostDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection (PostgreSQL).");

        // Replace registrations contributed by Core and DigitalProcesses without
        // changing either module. Their previously-registered options are harmless;
        // their context services are redirected to this one scoped instance.
        services.RemoveAll<CoreDbContext>();
        services.AddDbContext<HostDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<CoreDbContext>(sp => sp.GetRequiredService<HostDbContext>());
        return services;
    }
}
