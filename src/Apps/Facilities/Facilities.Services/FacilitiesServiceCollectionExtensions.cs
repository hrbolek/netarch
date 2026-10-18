using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Facilities;

public static class FacilitiesServiceCollectionExtensions
{
    /// <summary>Events services and the composed CoreDbContext/authorization layer must already be registered.</summary>
    public static IServiceCollection AddFacilitiesServices(this IServiceCollection services)
    {
        services.AddScoped<FacilityPermissionTree>();
        services.AddScoped<FacilityService>();
        services.AddScoped<IFacilityService>(sp => sp.GetRequiredService<FacilityService>());
        services.AddScoped<FacilityTypeService>();
        services.AddScoped<IFacilityTypeService>(sp => sp.GetRequiredService<FacilityTypeService>());
        services.AddScoped<ReservationService>();
        services.AddScoped<IReservationService>(sp => sp.GetRequiredService<ReservationService>());
        return services;
    }
}
