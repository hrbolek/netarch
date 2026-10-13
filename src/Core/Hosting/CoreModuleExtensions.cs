using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Hosting;

public static class CoreModuleExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
        => services.AddArchitectureModule<CoreModule>();

    public static WebApplication MapCore(this WebApplication app)
    {
        app.UseEasyEntraHeaderPrincipal();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapArchitectureModule<CoreModule>("architecture");
        return app;
    }
}
