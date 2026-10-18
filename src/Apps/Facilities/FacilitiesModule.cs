using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.UI.Routing;
using WebAppArchitecture.Hosting;

namespace WebAppArchitecture.Facilities;

public sealed class FacilitiesModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddFacilitiesServices();
        services.AddHostedService<FacilitiesDatabaseHostedService>();
    }

    public void ConfigureDatabase(ModelBuilder builder) => builder.ConfigureFacilities();

    public void ConfigureNavigation(IApplicationNavigationRegistry navigation)
    {
        navigation.Register(new ApplicationDefinition(
            "facilities",
            "Facilities",
            "/facilities/FacilityResponse/list",
            "Physical facilities, facility types, and event reservations.",
            new ApplicationNavigationItem[]
            {
                new("Facilities", Children: new ApplicationNavigationItem[]
                {
                    new("List", "/facilities/FacilityResponse/list"),
                    new("Tree", "/facilities/FacilityResponse/tree"),
                    new("Create facility", "/facilities/FacilityResponse/new"),
                    new("Facility types", "/facilities/FacilityTypeResponse/list"),
                    new("Create facility type", "/facilities/FacilityTypeResponse/new"),
                    new("Request reservation", "/facilities/FacilityReservationResponse/new")
                })
            }));
    }

    public void MapAPI(IEndpointRouteBuilder endpoints, string prefix)
        => FacilitiesEndpointExtensions.MapApi(endpoints, prefix);

    public void MapUI(IEndpointRouteBuilder endpoints, string prefix)
        => FacilitiesEndpointExtensions.MapUi(endpoints, prefix);
}

public static class FacilitiesExtensions
{
    public static IServiceCollection AddFacilities(this IServiceCollection services)
        => services.AddArchitectureModule<FacilitiesModule>();
}
