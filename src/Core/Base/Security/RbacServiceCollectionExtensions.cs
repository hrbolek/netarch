using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Base.Security;

public static class RbacServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteRbac(this IServiceCollection services, string connectionString, string? userStoreConnectionString = null)
    {
        services.AddSingleton<ICurrentActorProvider, HeaderOrClaimActorProvider>();
        services.AddSingleton<SqliteRbacService>(sp => new SqliteRbacService(
            connectionString,
            sp.GetRequiredService<ICurrentActorProvider>(),
            userStoreConnectionString));
        services.AddSingleton<IRbacService>(sp => sp.GetRequiredService<SqliteRbacService>());
        services.AddSingleton<IRbacAuthorizationService>(sp => sp.GetRequiredService<SqliteRbacService>());
        services.AddSingleton<IRbacAdminService>(sp => sp.GetRequiredService<SqliteRbacService>());
        services.AddSingleton<IRbacPermissionQueryService>(sp => sp.GetRequiredService<SqliteRbacService>());

        return services;
    }
}
