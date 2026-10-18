using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.GroupType.Models;
using WebAppArchitecture.GroupType.Resolvers;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace Core.Host.Integration.Tests;

public sealed class GroupTypeIntegrationTests
{
    [Fact]
    public async Task ExtractedGroupTypePreservesRoutesAndSecuredCrud()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var id = Guid.NewGuid();
        var actor = RbacWellKnown.BootstrapAdminUserId;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var now = DateTimeOffset.UtcNow;
            db.GroupTypes.Add(new GroupTypeModel(id, "Layout test", null, $"/{id:N}",
                RbacWellKnown.RootSecurityObjectId, now, now, actor, actor));
            await db.SaveChangesAsync();
        }

        const string prefix = "/architecture/GroupTypeResponse";
        var routes = host.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith(prefix, StringComparison.Ordinal) == true)
            .ToArray();
        Assert.Equal(new[]
        {
            $"DELETE {prefix}/delete/{{id:guid}}", $"GET {prefix}/link/{{id:guid}}",
            $"GET {prefix}/list", $"GET {prefix}/read/{{id:guid}}", $"PUT {prefix}/update"
        }, routes.Select(endpoint =>
            $"{Assert.Single(endpoint.Metadata.GetRequiredMetadata<HttpMethodMetadata>().HttpMethods)} {endpoint.RoutePattern.RawText}")
            .Order().ToArray());
        Assert.All(routes, endpoint => Assert.Equal(typeof(BaseResolvers),
            endpoint.Metadata.GetRequiredMetadata<MethodInfo>().DeclaringType));

        using var listRequest = ActingRequest.Get(prefix + "/list", actor);
        using var list = await client.SendAsync(listRequest);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.NotNull(await list.Content.ReadFromJsonAsync<GroupTypeResponse[]>());
        using var readRequest = ActingRequest.Get(prefix + "/read/" + id, actor);
        using var read = await client.SendAsync(readRequest);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var original = (await read.Content.ReadFromJsonAsync<GroupTypeResponse>())!;
        Assert.Equal("Layout test", original.Name);

        using var deniedRequest = ActingRequest.Get(prefix + "/read/" + id, Guid.NewGuid());
        using var denied = await client.SendAsync(deniedRequest);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        using var anonymousUpdate = await client.PutAsJsonAsync(prefix + "/update", new GroupTypeUpdateRequest(id, original.updated_at, "Denied"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousUpdate.StatusCode);

        using var updateRequest = ActingRequest.Get(prefix + "/update", actor);
        updateRequest.Method = HttpMethod.Put;
        updateRequest.Content = JsonContent.Create(new GroupTypeUpdateRequest(id, original.updated_at, "Updated"));
        using var update = await client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Updated", (await update.Content.ReadFromJsonAsync<GroupTypeResponse>())!.Name);

        using var linkRequest = ActingRequest.Get(prefix + "/link/" + id, actor);
        using var link = await client.SendAsync(linkRequest);
        Assert.Equal(HttpStatusCode.OK, link.StatusCode);

        using var deleteRequest = ActingRequest.Get(prefix + "/delete/" + id, actor);
        deleteRequest.Method = HttpMethod.Delete;
        using var delete = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var verifyRequest = ActingRequest.Get(prefix + "/read/" + id, actor);
        using var verify = await client.SendAsync(verifyRequest);
        Assert.Equal(HttpStatusCode.NotFound, verify.StatusCode);
    }
}
