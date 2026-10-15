using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.RequestTransitionHistory;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.Models.SubmissionNode;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Net.Http.Json;
using System.Text.Json;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

/// <summary>
/// App-specific negative runtime integration tests. Assert persistent effects,
/// not solely route registration or HTTP status codes.
/// </summary>
public sealed class DigitalProcessesCommandAtomicityTests
{
    [Fact]
    public async Task DP01_Start_UnknownProcessVersion_DoesNotCreateRuntimeRows()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var actor = Guid.NewGuid();
        using var command = new HttpRequestMessage(HttpMethod.Post, "/digital-processes/RequestResponse/start")
        {
            Content = JsonContent.Create(new { process_definition_version_id = Guid.NewGuid(), subject = "invalid version" })
        };
        command.Headers.Add("x-ms-client-principal-id", actor.ToString());
        using var response = await client.SendAsync(command);
        Assert.False(response.IsSuccessStatusCode);
        await AssertRuntimeEmpty(client, actor);
    }

    [Fact]
    public async Task DP02_Transition_UnknownRequest_DoesNotCreateHistoryOrSubmission()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var actor = Guid.NewGuid();
        using var command = new HttpRequestMessage(HttpMethod.Post,
            $"/digital-processes/RequestResponse/transition/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new { transition_id = Guid.NewGuid(), note = "invalid request" })
        };
        command.Headers.Add("x-ms-client-principal-id", actor.ToString());
        using var response = await client.SendAsync(command);
        Assert.False(response.IsSuccessStatusCode);
        await AssertRuntimeEmpty(client, actor);
    }

    [Fact]
    public async Task DP03_TwoIndependentFailedCommands_DoNotLeavePartialRuntimeData()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var actor = Guid.NewGuid();
        for (var i = 0; i < 2; i++)
        {
            using var command = new HttpRequestMessage(HttpMethod.Post, "/digital-processes/RequestResponse/start")
            {
                Content = JsonContent.Create(new { process_definition_version_id = Guid.NewGuid(), subject = $"invalid-{i}" })
            };
            command.Headers.Add("x-ms-client-principal-id", actor.ToString());
            using var response = await client.SendAsync(command);
            Assert.False(response.IsSuccessStatusCode);
        }
        await AssertRuntimeEmpty(client, actor);
    }

    private static async Task AssertRuntimeEmpty(HttpClient client, Guid actor)
    {
        foreach (var path in new[]
        {
            "/digital-processes/RequestResponse/list",
            "/digital-processes/SubmissionResponse/list",
            "/digital-processes/SubmissionNodeResponse/list",
            "/digital-processes/RequestTransitionHistoryResponse/list"
        })
        {
            using var response = await client.SendAsync(ActingRequest.Get(path, actor));
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
            Assert.Empty(document.RootElement.EnumerateArray());
        }
    }
}
