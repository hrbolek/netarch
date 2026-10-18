using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Resolvers;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Tests.Shared;
using WebAppArchitecture.User.Models;
using Xunit;

namespace Core.Host.Integration.Tests;

public sealed class SecuredUpdateTests
{
    [Fact]
    public void EveryCoreUpdateRequestRequiresTokenAndExcludesWritableAuditMetadata()
    {
        foreach (var type in new[]
        {
            typeof(UserUpdateRequest),
            typeof(WebAppArchitecture.Group.Models.GroupUpdateRequest),
            typeof(WebAppArchitecture.GroupType.Models.GroupTypeUpdateRequest),
            typeof(WebAppArchitecture.Role.Models.RoleUpdateRequest),
            typeof(WebAppArchitecture.RoleType.Models.RoleTypeUpdateRequest),
            typeof(WebAppArchitecture.Membership.Models.MembershipUpdateRequest)
        })
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("{}", type));
            Assert.Equal(typeof(DateTimeOffset), type.GetProperty("updated_at")!.PropertyType);
            Assert.DoesNotContain(type.GetProperties(), property =>
                property.Name is "created_at" or "created_by" or "updated_by");
        }
    }

    [Fact]
    public async Task JsonAndFormUpdatesRequireOriginalTokenAndKeepAuditServerOwned()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var id = Guid.NewGuid();
        var actor = RbacWellKnown.BootstrapAdminUserId;
        var creator = Guid.NewGuid();
        const string prefix = "/architecture/UserResponse";
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var now = DateTimeOffset.UtcNow.AddDays(-1);
            db.Users.Add(new UserModel(id, "Original", "Original surname", "original@example.test",
                RbacWellKnown.RootSecurityObjectId, now, now, creator, creator));
            await db.SaveChangesAsync();
            Assert.All(db.Model.GetEntityTypes().Where(entity =>
                typeof(ISecuredEntityDBModel).IsAssignableFrom(entity.ClrType)), entity =>
                    Assert.True(entity.FindProperty(nameof(ISecuredEntityDBModel.updated_at))!.IsConcurrencyToken));
        }

        var original = await Read();
        Assert.Equal(HttpStatusCode.BadRequest,
            await Put(new { id, Name = "Missing token" }));
        Assert.Equal(HttpStatusCode.BadRequest,
            await Put(new { id, Name = "Default token", updated_at = default(DateTimeOffset) }));
        Assert.Equal(HttpStatusCode.BadRequest,
            await Put(new { id, Name = "Invalid token", updated_at = "not-a-timestamp" }));
        Assert.Equal(HttpStatusCode.BadRequest, await Form("Missing token", null));

        Assert.Equal(HttpStatusCode.OK, await Put(new
        {
            id, Name = "First", updated_at = original.updated_at,
            created_at = DateTimeOffset.UtcNow.AddYears(1), created_by = Guid.NewGuid(),
            updated_by = Guid.NewGuid()
        }));
        var first = await Read();
        Assert.Equal("First", first.Name);
        Assert.Equal(original.created_at, first.created_at);
        Assert.Equal(creator, first.created_by);
        Assert.Equal(actor, first.updated_by);
        Assert.True(first.updated_at > original.updated_at);
        Assert.Equal(original.surname, first.surname);
        Assert.Equal(original.email, first.email);

        Assert.Equal(HttpStatusCode.Conflict,
            await Put(new UserUpdateRequest(id, original.updated_at, "Stale JSON")));
        Assert.Equal(HttpStatusCode.Conflict,
            await Form("Stale form", original.updated_at));
        Assert.Equal("First", (await Read()).Name);
        Assert.Equal(HttpStatusCode.OK, await Form("Second", first.updated_at));
        var second = await Read();
        Assert.Equal("Second", second.Name);
        Assert.True(second.updated_at > first.updated_at);
        Assert.Equal(actor, second.updated_by);
        Assert.Equal(original.created_at, second.created_at);
        Assert.Equal(creator, second.created_by);

        var updateRoute = host.Services.GetServices<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == prefix + "/update");
        Assert.Equal(typeof(BaseResolvers),
            updateRoute.Metadata.GetRequiredMetadata<System.Reflection.MethodInfo>().DeclaringType);

        async Task<UserResponse> Read()
        {
            using var request = ActingRequest.Get(prefix + "/view/" + id, actor);
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        }

        async Task<HttpStatusCode> Put<T>(T payload)
        {
            using var request = ActingRequest.Get(prefix + "/update", actor);
            request.Method = HttpMethod.Put;
            request.Content = JsonContent.Create(payload);
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        async Task<HttpStatusCode> Form(string name, DateTimeOffset? token)
        {
            using var request = ActingRequest.Get(prefix + "/update/" + id, actor);
            request.Method = HttpMethod.Post;
            var fields = new Dictionary<string, string> { ["Name"] = name };
            if (token is not null)
                fields["updated_at"] = token.Value.ToString("O");
            request.Content = new FormUrlEncodedContent(fields);
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }
    }
}
