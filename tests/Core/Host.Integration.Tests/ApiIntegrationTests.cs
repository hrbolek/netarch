using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Core.Host.Integration.Tests;

using WebAppArchitecture.Tests.Shared;

/// <summary>
/// Real HTTP -> Minimal API -> registered services -> SecurityLayer -> EF Core -> isolated SQLite.
/// No database provider-specific SQL is used in tests. Provider matrix belongs to Stage 99.
/// A new web host and isolated database are created for each test.
/// </summary>
public sealed class CoreHostIntegrationTests
{
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
        using var response = await client.SendAsync(ActingRequest.Get("/info", actor));
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
