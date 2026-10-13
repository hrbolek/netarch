using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Hosting;

namespace RootNamespace.AppName;

public sealed class AppNameModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IFirstEntityService, FirstEntityService>();
        services.AddScoped<ISecondEntityService, SecondEntityService>();
    }

    public void ConfigureDatabase(ModelBuilder modelBuilder)
        => new AppNameModelContributor().Configure(modelBuilder);

    public void MapAPI(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup("/" + prefix.Trim('/'));
        group.MapFirstEntityEndpoints();
        group.MapSecondEntityEndpoints();
    }
    // No Razor renderer: IModule.MapUI defaults to the API mapping.
}

public static class AppNameModuleExtensions
{
    public static IServiceCollection AddAppName(this IServiceCollection services)
        => services.AddArchitectureModule<AppNameModule>();
    public static IEndpointRouteBuilder MapAppName(this IEndpointRouteBuilder endpoints)
        => endpoints.MapArchitectureModule<AppNameModule>("appname");
}
