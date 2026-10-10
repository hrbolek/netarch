using System.Net;
using System.Text.Json;
using Xunit;
using WebAppArchitecture.Tests.Shared;

namespace DigitalProcesses.Integration.Tests;

public sealed class DigitalProcessesHttpTests
{
    [Fact]
    public async Task T11_UnknownRequestId_IsNotReturnedToUnprivilegedActor()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.SendAsync(ActingRequest.Get($"/digital-processes/RequestResponse/view/{Guid.NewGuid()}", Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task T11_ApplicantPolicy_UnknownVersion_ReturnsNotFound()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.SendAsync(ActingRequest.Get($"/digital-processes/ProcessDefinitionVersionResponse/applicant-access/{Guid.NewGuid()}", Guid.NewGuid()));
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

    [Theory]
    [InlineData("/digital-processes/SubmissionResponse/view/")]
    [InlineData("/digital-processes/SubmissionNodeResponse/view/")]
    public async Task T11_InaccessibleOrAbsentEntity_IsNotReadable(string prefix)
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.SendAsync(ActingRequest.Get(prefix + Guid.NewGuid(), Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task T11_OpenApi_ContainsSubmissionLifecycleRoutes()
    {
        using var app = new IsolatedHost();
        using var client = app.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/digital-processes/SubmissionResponse/tree/{id}", out _));
        Assert.True(paths.TryGetProperty("/digital-processes/SubmissionNodeResponse/value/{id}", out _));
        Assert.True(paths.TryGetProperty("/digital-processes/SubmissionNodeResponse/subtree/{id}", out _));
    }
}
