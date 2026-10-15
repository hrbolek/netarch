using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.Submission;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Net;
using System.Text.Json;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class DigitalProcessesBulkTreeHttpContractTests
{
    [Fact]
    public async Task DP27_OpenApi_AdvertisesBothBatchTreePutCommands()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/digital-processes/FormVersionResponse/tree/{id}").TryGetProperty("put", out _));
        Assert.True(paths.GetProperty("/digital-processes/SubmissionResponse/tree/{id}").TryGetProperty("put", out _));
    }

    [Theory]
    [InlineData("/digital-processes/FormVersionResponse/tree/not-a-guid")]
    [InlineData("/digital-processes/SubmissionResponse/tree/not-a-guid")]
    public async Task DP28_BatchTree_InvalidIdentifiers_DoNotReachServices(string uri)
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, uri);
        request.Headers.Add("x-ms-client-principal-id", Guid.NewGuid().ToString());
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
