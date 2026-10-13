using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Hosting;

namespace WebAppArchitecture.HostManagement;

/// <summary>Optional host-level diagnostics. Never owns the root route.</summary>
public sealed class HostManagementModule : IModule
{
    public void MapAPI(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints.MapGroup("/" + prefix.Trim('/'));
        group.MapGet("/info", CoreHostResolvers.Info)
            .WithTags("HostManagement");

        // Identity claims and proxy headers are sensitive. Do not publish debug in production.
        var env = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (env.IsDevelopment())
        {
            group.MapGet("/debug", CoreHostResolvers.Debug)
                .WithTags("HostManagement");
        }
    }
}
