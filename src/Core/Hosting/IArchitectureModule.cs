using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.UI.Routing;

namespace WebAppArchitecture.Hosting;

/// <summary>
/// Explicit application-module contract used by the architecture host.
/// A module owns its DI registrations, endpoint mapping and navigation contribution.
/// </summary>
public interface IArchitectureModule
{
    void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration);

    void MapEndpoints(
        IEndpointRouteBuilder endpoints);

    void ConfigureNavigation(
        IApplicationNavigationRegistry navigation);
}
