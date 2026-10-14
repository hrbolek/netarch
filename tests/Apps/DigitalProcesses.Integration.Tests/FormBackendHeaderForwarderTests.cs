using System.Net.Http;
using Microsoft.AspNetCore.Http;
using WebAppArchitecture.DigitalProcesses.Services;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class FormBackendHeaderForwarderTests
{
    [Fact]
    public void Apply_ForwardsIncomingClientPrincipalHeadersWithoutConfiguration()
    {
        var incoming = new DefaultHttpContext().Request;
        incoming.Headers["x-ms-client-principal"] = "encoded-principal";
        incoming.Headers["x-ms-client-principal-id"] = "user-id";
        incoming.Headers["x-ms-client-principal-name"] = "user@example.test";
        using var outgoing = new HttpRequestMessage(HttpMethod.Post, "https://jsonata.example/graphql");

        FormBackendHeaderForwarder.Apply(incoming, outgoing, new FormDataBackendOptions());

        Assert.Equal("encoded-principal", Assert.Single(outgoing.Headers.GetValues("x-ms-client-principal")));
        Assert.Equal("user-id", Assert.Single(outgoing.Headers.GetValues("x-ms-client-principal-id")));
        Assert.Equal("user@example.test", Assert.Single(outgoing.Headers.GetValues("x-ms-client-principal-name")));
    }
}
