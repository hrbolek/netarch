using Microsoft.AspNetCore.Builder;
namespace WebAppArchitecture.Hosting;

/// <summary>Compatibility name for older application modules; use IModule for new modules.</summary>
[System.Obsolete("Use IModule instead.")]
public interface IArchitectureModule : IModule
{
    void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints);
    void IModule.MapAPI(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string prefix) => MapEndpoints(endpoints.MapGroup("/" + prefix.Trim('/')));
    void IModule.MapUI(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string prefix) => MapEndpoints(endpoints.MapGroup("/" + prefix.Trim('/')));
}
