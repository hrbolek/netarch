using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.UI.Routing;

namespace WebAppArchitecture.Hosting;

public static class ArchitectureModuleExtensions
{
    public static IServiceCollection AddArchitectureModule<TModule>(this IServiceCollection services)
        where TModule : class, IArchitectureModule, new()
    {
        if (services.Any(d => d.ServiceType == typeof(TModule)))
        {
            return services;
        }

        var configuration = services.GetArchitectureConfiguration();
        var module = new TModule();
        module.ConfigureServices(services, configuration);
        services.AddSingleton(module);
        return services;
    }

    public static IEndpointRouteBuilder MapArchitectureModule<TModule>(this IEndpointRouteBuilder endpoints)
        where TModule : class, IArchitectureModule
    {
        var module = endpoints.ServiceProvider.GetRequiredService<TModule>();
        var navigation = endpoints.ServiceProvider.GetRequiredService<IApplicationNavigationRegistry>();
        module.ConfigureNavigation(navigation);
        module.MapEndpoints(endpoints);
        return endpoints;
    }

    private static IConfiguration GetArchitectureConfiguration(this IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(IConfiguration));
        if (descriptor?.ImplementationInstance is IConfiguration configuration)
        {
            return configuration;
        }

        throw new InvalidOperationException(
            "IConfiguration is not registered as an instance. Create the host with ArchitectureHost.Create(args) before adding modules.");
    }
}
