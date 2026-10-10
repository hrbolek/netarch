using WebAppArchitecture.DigitalProcesses;
using WebAppArchitecture.Hosting;
#if JSONATA_PLAYGROUND
using WebAppArchitecture.JsonataPlayground;
#endif

var host = ArchitectureHost.Create(args);

host.Services
    .AddCore()
    .AddDigitalProcesses();

var app = host.Build();

app
    .MapCore()
    .MapDigitalProcesses();

#if JSONATA_PLAYGROUND
JsonataDiagnosticEndpoints.Map(app);
#endif

// Start the host first: endpoint data sources are finalized during application startup.
// Exporting before StartAsync() can produce an empty OpenAPI paths collection.
app.Urls.Add("http://0.0.0.0:8080");
await app.StartAsync();

// Disable with SWAGGER_EXPORT=false; change location with SWAGGER_EXPORT_PATH.
if (!string.Equals(Environment.GetEnvironmentVariable("SWAGGER_EXPORT"), "false", StringComparison.OrdinalIgnoreCase))
{
    await app.ExportSwaggerJsonAsync(
        Environment.GetEnvironmentVariable("SWAGGER_EXPORT_PATH") ?? "openapi/swagger.json");
}

await app.WaitForShutdownAsync();

// Expose the entry-point type to ASP.NET Core WebApplicationFactory integration tests.
public partial class Program { }
