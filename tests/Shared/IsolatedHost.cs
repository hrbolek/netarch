using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WebAppArchitecture.Tests.Shared;

/// <summary>HTTP integration host with one disposable PostgreSQL database per host.</summary>
public sealed class IsolatedHost : WebApplicationFactory<Program>
{
    private readonly PostgresTestDatabase database = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string,string?>
            {
                ["ConnectionStrings:DefaultConnection"] = database.ConnectionString,
                
                ["SystemData:Import"] = "false",
                ["SecurityProjection:RebuildOnStartup"] = "false",
                ["SWAGGER_EXPORT"] = "false"
            });
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) database.Dispose();
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
