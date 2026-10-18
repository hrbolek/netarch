using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.UI.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Hosting;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Security;
using Microsoft.AspNetCore.Mvc;
namespace WebAppArchitecture.Events;

public sealed class EventsModule : IModule
{
 public void ConfigureServices(IServiceCollection services,IConfiguration configuration)
 {
  services.AddScoped<EventTypeService>();
  services.AddScoped<EventService>();
  services.AddScoped<EventAdministrationService>();
  services.AddHostedService<EventsDatabaseHostedService>();
 }
 public void ConfigureDatabase(ModelBuilder builder) => builder.ConfigureEvents();
 public void ConfigureNavigation(IApplicationNavigationRegistry navigation)
 {
  navigation.Register(new ApplicationDefinition("events", "Events", "/events/EventResponse/list",
   "Události, akademické roky, semestry a výukové aktivity.",
   new ApplicationNavigationItem[] {
    new("Události", Children: new ApplicationNavigationItem[] {
     new("Seznam", "/events/EventResponse/list"),
     new("Nová událost", "/events/EventResponse/new"),
     new("Typy událostí", "/events/EventTypeResponse/list"),
     new("Nový typ události", "/events/EventTypeResponse/new"),
     new("Administrace", "/events/EventResponse/admin"),
     new("Typy událostí (JSON)", "/events/EventTypeResponse/tree")
    })
   }));
 }
 public void MapAPI(IEndpointRouteBuilder endpoints,string prefix) => EventsEndpointExtensions.MapApi(endpoints,prefix);
 public void MapUI(IEndpointRouteBuilder endpoints,string prefix) => EventsEndpointExtensions.MapUi(endpoints,prefix);


}
public static class EventsExtensions
{
 public static IServiceCollection AddEvents(this IServiceCollection services)=>services.AddArchitectureModule<EventsModule>();
}
