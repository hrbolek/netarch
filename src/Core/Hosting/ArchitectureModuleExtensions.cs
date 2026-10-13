using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.UI.Routing;

namespace WebAppArchitecture.Hosting;

public static class ArchitectureModuleExtensions
{
    public static IServiceCollection AddArchitectureModule<TModule>(this IServiceCollection services)
        where TModule : class, IModule, new()
    {
        if (services.Any(d => d.ServiceType == typeof(TModule))) return services;
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(IConfiguration));
        if (descriptor?.ImplementationInstance is not IConfiguration configuration)
            throw new InvalidOperationException("Register IConfiguration before adding modules.");
        var module = new TModule();
        module.ConfigureServices(services, configuration);
        services.AddSingleton(module);
        services.AddSingleton<IModule>(sp => sp.GetRequiredService<TModule>());
        return services;
    }

    public static IEndpointRouteBuilder MapArchitectureModule<TModule>(
        this IEndpointRouteBuilder endpoints, string prefix, ModuleMode mode = ModuleMode.Ui)
        where TModule : class, IModule
    {
        var module = endpoints.ServiceProvider.GetRequiredService<TModule>();
        var navigation = endpoints.ServiceProvider.GetRequiredService<IApplicationNavigationRegistry>();
        module.ConfigureNavigation(navigation);
        if (mode == ModuleMode.Ui) module.MapUI(endpoints, prefix);
        else module.MapAPI(endpoints, prefix);
        return endpoints;
    }

    /// <summary>Configure all installed modules' HTTP pipeline contributions once, in registration order.</summary>
    public static WebApplication ConfigureModules(this WebApplication app)
    {
        foreach (var module in app.Services.GetServices<IModule>())
            module.ConfigureApplication(app);
        return app;
    }
    /// <summary>Map installed modules once, with prefixes explicitly chosen by the Host.</summary>
    public static WebApplication MapModules(
        this WebApplication app,
        IReadOnlyDictionary<Type, string> prefixes,
        ModuleMode mode = ModuleMode.Ui)
    {
        var navigation = app.Services.GetRequiredService<IApplicationNavigationRegistry>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in app.Services.GetServices<IModule>())
        {
            if (!prefixes.TryGetValue(module.GetType(), out var prefix))
                throw new InvalidOperationException($"Missing route prefix for {module.GetType().Name}.");
            prefix = prefix.Trim('/');
            if (string.IsNullOrWhiteSpace(prefix) || prefix.Contains('/'))
                throw new ArgumentException($"Module prefix must be a single URI segment: '{prefix}'.");
            if (!used.Add(prefix))
                throw new InvalidOperationException($"Duplicate module route prefix: '{prefix}'.");
            module.ConfigureNavigation(navigation);
            if (mode == ModuleMode.Ui) module.MapUI(app, prefix);
            else module.MapAPI(app, prefix);
        }
        return app;
    }
}
