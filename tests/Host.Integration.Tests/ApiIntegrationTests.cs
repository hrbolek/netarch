using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Host.Integration.Tests;

/// <summary>
/// Real HTTP -> Minimal API -> registered services -> SecurityLayer -> EF Core -> isolated SQLite.
/// No database provider-specific SQL is used in tests. Provider matrix belongs to Stage 99.
/// A new web host and isolated database are created for each test.
/// </summary>
public sealed class ApiIntegrationTests
{
    private sealed class IsolatedHost : WebApplicationFactory<Program>
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "za78-stage3-" + Guid.NewGuid().ToString("N"));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(directory);
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={Path.Combine(directory, "test.db")}",
                    ["DatabaseDump:Path"] = Path.Combine(directory, "no-dump.json"),
                    ["SystemData:Import"] = "false",
                    ["SecurityProjection:RebuildOnStartup"] = "false",
                    ["SWAGGER_EXPORT"] = "false"
                });
            });
        }
    }

    private static HttpRequestMessage GetAs(string uri, Guid actor)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("x-ms-client-principal-id", actor.ToString());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    [Fact]
    public async Task T11_InfoEndpoint_RespondsWithJSON()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("App běží", body.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task T13_IdentityFromTrustedProxyHeader_ReachesActualHost()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        var actor = Guid.NewGuid();
        using var response = await client.SendAsync(GetAs("/info", actor));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(actor.ToString(), body.RootElement.GetProperty("actor_user_id").GetString());
    }

    [Fact]
    public async Task T13_MissingIdentity_DoesNotProduceAnActorId()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("actor_user_id").ValueKind);
    }

    [Fact]
    public async Task T11_UnknownRequestId_IsNotReturnedToUnprivilegedActor()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.SendAsync(GetAs($"/digital-processes/RequestResponse/view/{Guid.NewGuid()}", Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task T11_ApplicantPolicy_UnknownVersion_ReturnsNotFound()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.SendAsync(GetAs($"/digital-processes/ProcessDefinitionVersionResponse/applicant-access/{Guid.NewGuid()}", Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task T11_OpenApi_RegistersRuntimeAndPolicyRoutes()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = body.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/digital-processes/RequestResponse/start", out _));
        Assert.True(paths.TryGetProperty("/digital-processes/RequestResponse/transition/{id}", out _));
        Assert.True(paths.TryGetProperty("/digital-processes/ProcessDefinitionVersionResponse/applicant-access/{processVersionId}", out _));
    }
}
