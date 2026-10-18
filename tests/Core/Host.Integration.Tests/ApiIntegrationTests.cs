using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses;
using WebAppArchitecture.Host;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WebAppArchitecture.Base.UI.Routing;
using Xunit;

namespace Core.Host.Integration.Tests;

using WebAppArchitecture.Tests.Shared;

/// <summary>
/// Real HTTP -> Minimal API -> registered services -> AuthorizationLayer -> EF Core -> isolated SQLite.
/// No database provider-specific SQL is used in tests. Provider matrix belongs to Stage 99.
/// A new web host and isolated database are created for each test.
/// </summary>
public sealed class CoreHostIntegrationTests
{
    [Fact]
    public void T20_AllApplicationLayersShareOneScopedEfContext()
    {
        using var app = new IsolatedHost();
        using var scope = app.Services.CreateScope();
        var hostDb = scope.ServiceProvider.GetRequiredService<HostDbContext>();
        Assert.Same(hostDb, scope.ServiceProvider.GetRequiredService<CoreDbContext>());
        Assert.NotNull(hostDb.Model.FindEntityType(typeof(WebAppArchitecture.DigitalProcesses.Models.FormDefinition.FormDefinitionModel)));
    }

    [Fact]
    public async Task T21_LegacyInfoPath_IsNotRegistered()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/info");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task T22_HostRootListsRegisteredApplicationsAsJsonAndCards()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<title>Home</title>", html);
        Assert.Contains("Select an application area.", html);
        var applications = app.Services.GetRequiredService<IApplicationNavigationRegistry>().GetApplications();
        Assert.NotEmpty(applications);
        Assert.Contains(applications, application => application.RootUrl == "/digital-processes");
        foreach (var application in applications)
        {
            Assert.Contains($"<h2 class=\"h5 card-title\">{WebUtility.HtmlEncode(application.Label)}</h2>", html);
            Assert.Contains($"href=\"{WebUtility.HtmlEncode(application.RootUrl)}\">Open</a>", html);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Accept.ParseAdd("application/json");
        using var jsonResponse = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, jsonResponse.StatusCode);
        var jsonApplications = await jsonResponse.Content.ReadFromJsonAsync<ApplicationDefinition[]>();
        Assert.NotNull(jsonApplications);
        Assert.Equal(applications.Select(application => application.Key), jsonApplications.Select(application => application.Key));
        Assert.Equal(applications.Select(application => application.RootUrl), jsonApplications.Select(application => application.RootUrl));
    }

    [Fact]
    public async Task T11_InfoEndpoint_RespondsWithJSON()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/management/info");
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
        using var response = await client.SendAsync(ActingRequest.Get("/management/info", actor));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(actor.ToString(), body.RootElement.GetProperty("actor_user_id").GetString());
    }

    [Fact]
    public async Task T13_MissingIdentity_DoesNotProduceAnActorId()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/management/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("actor_user_id").ValueKind);
    }

    [Fact]
    public async Task T11_CoreSwagger_ExposesInfo()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("paths", out _));
    }
}
