using Microsoft.AspNetCore.Http;
using WebAppArchitecture.DigitalProcesses;
using WebAppArchitecture.Host;
using WebAppArchitecture.Hosting;
using WebAppArchitecture.HostManagement;
using WebAppArchitecture.Events;
using WebAppArchitecture.Facilities;
#if !ARCHITECTURE_API_ONLY
using WebAppArchitecture.Base.Endpoints;
using WebAppArchitecture.Base.UI.Pages;
using WebAppArchitecture.Base.UI.Routing;
#endif
#if JSONATA_PLAYGROUND
using WebAppArchitecture.JsonataPlayground;
#endif

// Module selection and URI prefixes are part of this executable's source, not runtime configuration.
var builder = ArchitectureHost.Create(args);

builder.Services
    .AddCore()
    .AddDigitalProcesses()
    .AddEvents()
    .AddFacilities()
    .AddArchitectureModule<HostManagementModule>();

#if !ARCHITECTURE_API_ONLY
builder.Services.AddRazorComponents();
#endif

// The Host owns the shared EF context; domain modules supply EF model mappings.
builder.Services.AddHostDatabase(builder.Configuration);

var app = builder.Build();
#if !ARCHITECTURE_API_ONLY
app.UseStaticFiles();
#endif
app.ConfigureModules();

#if ARCHITECTURE_API_ONLY
app.MapModules(ModulePrefixes, ModuleMode.Api);
#else
app.MapModules(ModulePrefixes, ModuleMode.Ui);
app.MapGet("/architecture", CoreHostResolvers.Architecture)
    .ApplyViewModel<ApplicationDashboardPage, ApplicationDefinition>()
    .WithTags("Home");
app.MapGet("/", CoreHostResolvers.Home)
    .ApplyViewModel<ApplicationHomePage, ApplicationDefinition[]>()
    .WithTags("Home");
#endif

#if JSONATA_PLAYGROUND
JsonataDiagnosticEndpoints.Map(app);
#endif

app.Urls.Add("http://0.0.0.0:8080");
await app.StartAsync();

// Existing development OpenAPI export behavior is retained.
if (!string.Equals(Environment.GetEnvironmentVariable("SWAGGER_EXPORT"), "false", StringComparison.OrdinalIgnoreCase))
    await app.ExportSwaggerJsonAsync(
        Environment.GetEnvironmentVariable("SWAGGER_EXPORT_PATH") ?? "openapi/swagger.json");

await app.WaitForShutdownAsync();

// WebApplicationFactory integration tests depend on this entry-point type.
public partial class Program
{
    private static readonly IReadOnlyDictionary<Type, string> ModulePrefixes = new Dictionary<Type, string>
    {
        [typeof(CoreModule)] = "architecture",
        [typeof(DigitalProcessesModule)] = "digital-processes",
        [typeof(EventsModule)] = "events",
        [typeof(FacilitiesModule)] = "facilities",
        [typeof(HostManagementModule)] = "management"
    };
}
