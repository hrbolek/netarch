using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Routing;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.DigitalProcesses.Resolvers;
using WebAppArchitecture.DigitalProcesses.Services;
using WebAppArchitecture.DigitalProcesses.UI.Routing;
using WebAppArchitecture.Hosting;

namespace WebAppArchitecture.DigitalProcesses;

public static class DigitalProcessesModuleExtensions
{
    public static IServiceCollection AddDigitalProcesses(this IServiceCollection services)
        => services.AddArchitectureModule<DigitalProcessesModule>();

    public static IEndpointRouteBuilder MapDigitalProcesses(this IEndpointRouteBuilder endpoints)
        => endpoints.MapArchitectureModule<DigitalProcessesModule>();
}
