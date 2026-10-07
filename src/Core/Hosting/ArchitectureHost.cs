using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.UI.Routing;

namespace WebAppArchitecture.Hosting;

/// <summary>
/// Thin wrapper around WebApplicationBuilder that installs only host-level infrastructure.
/// Domain/Core modules remain explicit through AddCore/AddX and MapCore/MapX.
/// </summary>
public sealed class ArchitectureHost
{
    private readonly WebApplicationBuilder builder;

    private ArchitectureHost(WebApplicationBuilder builder)
    {
        this.builder = builder;
        ConfigureHostInfrastructure(builder.Services, builder.Configuration);
    }

    public IServiceCollection Services => builder.Services;
    public IConfiguration Configuration => builder.Configuration;

    public static ArchitectureHost Create(string[] args)
        => new(WebApplication.CreateBuilder(args));

    public WebApplication Build()
        => builder.Build();

    private static void ConfigureHostInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        // Keep the builder configuration directly available to pre-Build module registration.
        services.AddSingleton<IConfiguration>(configuration);
        services.AddEndpointsApiExplorer();
        services.AddRazorComponents();
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<ArchitectureOperationFilter>();
        });
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddSingleton<EntityRouteRegistry>();
        services.AddSingleton<IEntityRouteRegistry>(sp => sp.GetRequiredService<EntityRouteRegistry>());
        services.AddSingleton<ApplicationNavigationRegistry>();
        services.AddSingleton<IApplicationNavigationRegistry>(sp => sp.GetRequiredService<ApplicationNavigationRegistry>());
    }
}
