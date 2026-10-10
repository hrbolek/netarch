using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WebAppArchitecture.Tests.Shared;

/// <summary>HTTP integration host shared by Core and app suites; isolated SQLite file per host.</summary>
public sealed class IsolatedHost : WebApplicationFactory<Program>
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "za78-stage3-" + Guid.NewGuid().ToString("N"));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(directory);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string,string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={Path.Combine(directory,"test.db")}",
                ["DatabaseDump:Path"] = Path.Combine(directory,"no-dump.json"),
                ["SystemData:Import"] = "false",
                ["SecurityProjection:RebuildOnStartup"] = "false",
                ["SWAGGER_EXPORT"] = "false"
            });
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try { Directory.Delete(directory, recursive:true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}

public static class ActingRequest
{
    public static HttpRequestMessage Get(string uri, Guid actor)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("x-ms-client-principal-id", actor.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}
