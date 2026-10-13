using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.UI.Routing;

namespace WebAppArchitecture.Hosting;

/// <summary>Simple compile-time composition contract. Phases are called in order by the Host.</summary>
public interface IModule
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    void ConfigureDatabase(ModelBuilder modelBuilder) { }
    void ConfigureApplication(WebApplication app) { }
    void ConfigureNavigation(IApplicationNavigationRegistry navigation) { }
    /// <summary>Register one representation per HTTP method and route.</summary>
    void MapAPI(IEndpointRouteBuilder endpoints, string prefix);
    /// <summary>Default UI mapping is the API mapping for modules without Razor views.</summary>
    void MapUI(IEndpointRouteBuilder endpoints, string prefix) => MapAPI(endpoints, prefix);
}

public enum ModuleMode { Api, Ui }
