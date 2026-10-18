using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed class ResolverBoundaryHost : WebApplicationFactory<Program>
{
    private readonly Dictionary<Type, (object Service, StrictServiceSpy Spy)> spies = new();
    private readonly ConcurrentQueue<string> forbidden = new();

    public StrictServiceSpy Spy<T>() where T : class
    {
        if (!spies.TryGetValue(typeof(T), out var entry))
        {
            var (service, spy) = StrictServiceSpy.Create<T>();
            entry = (service, spy);
            spies.Add(typeof(T), entry);
        }
        return entry.Spy;
    }

    internal StrictServiceSpy RegisteredSpy(Type contract)
        => spies.TryGetValue(contract, out var entry) ? entry.Spy
            : throw new InvalidOperationException($"No strict spy registered for {contract}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=unused.invalid;Database=forbidden;Username=forbidden",
                ["SystemData:Import"] = "false",
                ["SecurityProjection:RebuildOnStartup"] = "false"
            }));
        builder.ConfigureTestServices(services =>
        {
            // Only application background/database initialization is removed. Program,
            // module middleware, route registrations and endpoint filters remain real.
            foreach (var descriptor in services.Where(descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType?.Namespace?.StartsWith("WebAppArchitecture.", StringComparison.Ordinal) == true).ToArray())
                services.Remove(descriptor);

            foreach (var type in services.Select(descriptor => descriptor.ServiceType).Distinct().ToArray())
            {
                if (type.ContainsGenericParameters)
                    continue;
                if (IsForbidden(type))
                {
                    services.RemoveAll(type);
                    services.AddScoped(type, _ => FailForbidden(type));
                }
                else if (IsServiceContract(type))
                {
                    if (!spies.ContainsKey(type))
                    {
                        var service = System.Reflection.DispatchProxy.Create(type, typeof(StrictServiceSpy));
                        spies.Add(type, (service, (StrictServiceSpy)service));
                    }
                    var entry = spies[type];
                    services.RemoveAll(type);
                    services.AddScoped(type, provider =>
                    {
                        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
                        entry.Spy.Bind(() => accessor.HttpContext);
                        return entry.Service;
                    });
                }
            }
        });
    }

    private object FailForbidden(Type type)
    {
        var message = $"Resolver attempted forbidden dependency: {type.FullName}";
        forbidden.Enqueue(message);
        throw new InvalidOperationException(message);
    }

    internal static bool IsForbidden(Type type)
        => CompiledBoundaryInspector.IsPersistence(type);

    internal static bool IsServiceContract(Type type)
        => type.IsInterface && (type == typeof(IRbacAuthorizationService) ||
           type.Assembly.GetName().Name?.EndsWith(".Services", StringComparison.Ordinal) == true ||
           type.GetInterfaces().Append(type).Any(contract => contract.IsGenericType &&
               contract.GetGenericTypeDefinition() == typeof(ISecuredEntityService<>)));

    public void Verify()
    {
        Assert.Empty(forbidden);
        foreach (var entry in spies.Values)
            entry.Spy.Verify();
    }
}
