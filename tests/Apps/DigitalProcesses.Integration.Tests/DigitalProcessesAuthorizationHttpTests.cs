using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Net;
using System.Text.Json;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

/// <summary>
/// Real HTTP integration checks over an isolated database. These tests explicitly
/// do not claim to cover a successful Request -> Submission -> transition lifecycle.
/// </summary>
public sealed class DigitalProcessesAuthorizationHttpTests
{
    [Theory]
    [InlineData("/digital-processes/RequestResponse/list")]
    [InlineData("/digital-processes/SubmissionResponse/list")]
    [InlineData("/digital-processes/SubmissionNodeResponse/list")]
    public async Task T11_EmptyDatabase_DoesNotRevealAnyRuntimeEntity(string path)
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        using var response = await client.SendAsync(ActingRequest.Get(path, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Empty(document.RootElement.EnumerateArray());
    }

    [Theory]
    [InlineData("/digital-processes/RequestResponse/list")]
    [InlineData("/digital-processes/SubmissionNodeResponse/list")]
    public async Task T11_List_RejectsUnknownRbacOperation(string path)
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        using var response = await client.SendAsync(ActingRequest.Get(path + "?operation=nonexistent_permission", Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Unknown RBAC operation", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task T11_OpenApi_DeclaresLifecycleHttpMethods()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/digital-processes/RequestResponse/start").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/digital-processes/RequestResponse/transition/{id}").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/digital-processes/SubmissionNodeResponse/value/{id}").TryGetProperty("put", out _));
        Assert.True(paths.GetProperty("/digital-processes/SubmissionNodeResponse/subtree/{id}").TryGetProperty("get", out _));
    }
}
