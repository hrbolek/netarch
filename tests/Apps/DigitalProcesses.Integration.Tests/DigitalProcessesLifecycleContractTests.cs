using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Net;
using System.Text.Json;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

/// <summary>
/// Integration tests for runtime HTTP contract and cross-host isolation.
/// These are NOT a substitute for a published-process start/transition scenario.
/// </summary>
public sealed class DigitalProcessesLifecycleContractTests
{
    [Theory]
    [InlineData("POST", "/digital-processes/RequestResponse/transition/not-a-guid")]
    [InlineData("GET", "/digital-processes/RequestResponse/detail/not-a-guid")]
    [InlineData("GET", "/digital-processes/SubmissionResponse/tree/not-a-guid")]
    [InlineData("PUT", "/digital-processes/SubmissionNodeResponse/value/not-a-guid")]
    [InlineData("GET", "/digital-processes/SubmissionNodeResponse/subtree/not-a-guid")]
    public async Task T11_InvalidIdentifier_IsRejectedByRouting(string method, string uri)
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), uri);
        request.Headers.Add("x-ms-client-principal-id", Guid.NewGuid().ToString());
        using var response = await client.SendAsync(request);
        // The route has a {id:guid} constraint. With the correct HTTP verb,
        // a non-GUID path never matches it, so routing must return 404.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task T11_SeparateHosts_HaveIsolatedRuntimeData()
    {
        using var first = new IsolatedHost();
        using var second = new IsolatedHost();
        using var firstClient = first.CreateClient();
        using var secondClient = second.CreateClient();
        var actor = Guid.NewGuid();
        using var firstResponse = await firstClient.SendAsync(ActingRequest.Get("/digital-processes/RequestResponse/list", actor));
        using var secondResponse = await secondClient.SendAsync(ActingRequest.Get("/digital-processes/RequestResponse/list", actor));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using var firstJson = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        using var secondJson = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
        Assert.Empty(firstJson.RootElement.EnumerateArray());
        Assert.Empty(secondJson.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task T11_RuntimeCommands_ExposeDistinctHTTPMethodContracts()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/digital-processes/RequestResponse/start").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/digital-processes/RequestResponse/transition/{id}").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/digital-processes/SubmissionNodeResponse/create-in-submission").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/digital-processes/SubmissionNodeResponse/create-in-node").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/digital-processes/SubmissionNodeResponse/value/{id}").TryGetProperty("put", out _));
    }

    [Theory]
    [InlineData("/digital-processes/RequestResponse/start")]
    [InlineData("/digital-processes/SubmissionNodeResponse/create-in-submission")]
    [InlineData("/digital-processes/SubmissionNodeResponse/create-in-node")]
    public async Task T11_CreateCommands_AreNotInvokedThroughGET(string uri)
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        using var response = await client.SendAsync(ActingRequest.Get(uri, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
