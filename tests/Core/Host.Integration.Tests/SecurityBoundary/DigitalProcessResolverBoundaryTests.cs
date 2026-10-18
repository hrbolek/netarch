using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DigitalProcesses.Models.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Models.Request;
using WebAppArchitecture.DigitalProcesses.Models.Shared;
using WebAppArchitecture.DigitalProcesses.Services.FormDefinition;
using WebAppArchitecture.DigitalProcesses.Services.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.Request;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed class DigitalProcessResolverBoundaryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RuntimeCommandsUseRequestServiceAndCannotOverrideInitiator(bool transition)
    {
        using var host = new ResolverBoundaryHost();
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var related = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        host.Spy<IRequestService>().Expect(transition ? "TransitionAsync" : "StartAsync", call =>
        {
            Assert.Equal(actor, call.Actor);
            if (transition)
            {
                Assert.Equal(id, call.Arguments[0]);
                Assert.Equal(related, Assert.IsType<TransitionRequestRequest>(call.Arguments[1]).transition_id);
            }
            else
                Assert.Equal(related, Assert.IsType<StartRequestRequest>(call.Arguments[0]).process_definition_version_id);
            return Task.FromResult(new RequestResponse(id, related, actor, related, related, "request", related, now, now, actor, actor));
        });
        using var client = host.CreateClient();
        var path = transition ? $"/transition/{id}" : "/start";
        using var request = ActingRequest.Get("/digital-processes/RequestResponse" + path + "?actor_user_id=" + related, actor);
        request.Method = HttpMethod.Post;
        request.Content = JsonContent.Create(new
        {
            transition_id = related, process_definition_version_id = related,
            initiator_user_id = related, actor_user_id = related, rbacobject_id = related
        });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(actor, (await response.Content.ReadFromJsonAsync<RequestResponse>())!.initiator_user_id);
        host.Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeniedRuntimeCommandNeverReturnsSuccessOrInvokesFollowUpServices(bool transition)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IRequestService>().Expect(transition ? "TransitionAsync" : "StartAsync",
            _ => throw new UnauthorizedAccessException("denied"));
        using var client = host.CreateClient();
        using var request = ActingRequest.Get("/digital-processes/RequestResponse" +
            (transition ? "/transition/" + Guid.NewGuid() : "/start"), Guid.NewGuid());
        request.Method = HttpMethod.Post;
        request.Content = JsonContent.Create(new { transition_id = Guid.NewGuid(), process_definition_version_id = Guid.NewGuid() });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        host.Verify();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    public async Task AuxiliaryCustomResolverPreservesServiceOutcomeStatus(int status)
    {
        using var host = new ResolverBoundaryHost();
        var id = Guid.NewGuid();
        host.Spy<IFormDesignAiService>().Expect("DesignAsync", call =>
        {
            Assert.Equal(id, Assert.IsType<FormDesignRequest>(call.Arguments[0]).FormVersionId);
            return Task.FromResult(status == 200
                ? AuxiliaryOperationResult.Ok(new { proposed = true })
                : AuxiliaryOperationResult.Problem(status, "service outcome"));
        });
        using var client = host.CreateClient();
        using var request = ActingRequest.Get("/digital-processes/ai/form-design", Guid.NewGuid());
        request.Method = HttpMethod.Post;
        request.Content = JsonContent.Create(new FormDesignRequest(id, "draft"));
        using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task CreateUsesTypedServiceAndIgnoresActorAndScopeOverrides()
    {
        using var host = new ResolverBoundaryHost();
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        host.Spy<IFormDefinitionService>().Expect("CreateTypedAsync", call =>
        {
            var input = Assert.IsType<CreateFormDefinitionRequest>(call.Arguments[0]);
            Assert.Equal("form", input.name);
            Assert.Equal(actor, call.Actor);
            return Task.FromResult(new FormDefinitionResponse(id, "form", null, RbacWellKnown.FormDefinitionCreationScopeId, now, now, actor, actor));
        });
        using var client = host.CreateClient();
        using var request = ActingRequest.Get("/digital-processes/FormDefinitionResponse/create-typed?operation=read&actor_user_id=" + id, actor);
        request.Method = HttpMethod.Post;
        request.Content = JsonContent.Create(new { name = "form", actor_user_id = id, rbacobject_id = id });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(id, (await response.Content.ReadFromJsonAsync<FormDefinitionResponse>())!.id);
        host.Verify();
    }

    [Theory]
    [InlineData("argument", 400)]
    [InlineData("unauthorized", 500)]
    [InlineData("unexpected", 500)]
    public async Task ApiCreateNeverTurnsServiceFailureIntoSuccess(string failure, int status)
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFormDefinitionService>().Expect("CreateTypedAsync", _ => throw failure switch
        {
            "argument" => new ArgumentException("invalid form"),
            "unauthorized" => new UnauthorizedAccessException("denied"),
            _ => new InvalidOperationException("failed")
        });
        using var client = host.CreateClient();
        using var request = ActingRequest.Get("/digital-processes/FormDefinitionResponse/create-typed", Guid.NewGuid());
        request.Method = HttpMethod.Post;
        request.Content = JsonContent.Create(new CreateFormDefinitionRequest("form"));
        using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task FormCreateDenialIs403AndDoesNotRedirect()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFormDefinitionService>().Expect("CreateTypedAsync", _ => throw new UnauthorizedAccessException("denied"));
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = ActingRequest.Get("/digital-processes/FormDefinitionResponse/create-form", Guid.NewGuid());
        request.Method = HttpMethod.Post;
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["name"] = "form" });
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        host.Verify();
    }

    [Fact]
    public async Task CreatePermissionDelegatesOnlyToPermissionServiceMethod()
    {
        using var host = new ResolverBoundaryHost();
        host.Spy<IFormDefinitionService>().Expect("CanCreateAsync", _ => throw new UnauthorizedAccessException("missing actor"));
        using var client = host.CreateClient();
        using var response = await client.GetAsync("/digital-processes/FormDefinitionResponse/create-permission");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        host.Verify();
    }

    [Fact]
    public async Task NewVersionUsesVersionServiceAndTrustedParentId()
    {
        using var host = new ResolverBoundaryHost();
        var definition = Guid.NewGuid();
        var version = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        host.Spy<IFormVersionService>().Expect("CreateNextAsync", call =>
        {
            Assert.Equal(definition, call.Arguments[0]);
            Assert.Equal(actor, call.Actor);
            return Task.FromResult(new FormVersionResponse(version, definition, 1, false, null, definition, now, now, actor, actor));
        });
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = ActingRequest.Get($"/digital-processes/FormDefinitionResponse/new-version/{definition}?actor_user_id={version}", actor);
        request.Method = HttpMethod.Post;
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/digital-processes/FormVersionResponse/designer/{version}", response.Headers.Location?.OriginalString);
        host.Verify();
    }
}
