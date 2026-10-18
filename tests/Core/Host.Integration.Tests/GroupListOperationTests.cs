using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Tests.Shared;
using WebAppArchitecture.Group.Models;
using Xunit;

namespace Core.Host.Integration.Tests;

public sealed class GroupListOperationTests
{
    [Fact]
    public async Task ListUsesReadPermissionAndCreatePickerUsesCreatePermission()
    {
        using var host = new IsolatedHost();
        using var client = host.CreateClient();
        var actor = RbacWellKnown.BootstrapAdminUserId;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var listOperationId = RbacOperationIds.List;
            var listGrants = await db.RoleTypeOperations
                .Where(link => link.operation_id == listOperationId)
                .ToArrayAsync();
            db.RoleTypeOperations.RemoveRange(listGrants);
            await db.SaveChangesAsync();
        }
        using (var scope = host.Services.CreateScope())
        {
            var authorization = scope.ServiceProvider.GetRequiredService<IRbacAuthorizationService>();
            Assert.False(await authorization.IsAllowedAsync(actor, RbacWellKnown.RootGroupId, RbacOperations.List));
            Assert.True(await authorization.IsAllowedAsync(actor, RbacWellKnown.RootGroupId, RbacOperations.Read));
        }

        const string prefix = "/architecture/GroupResponse";
        using var listRequest = ActingRequest.Get(prefix + "/list?take=20", actor);
        using var list = await client.SendAsync(listRequest);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.NotEmpty((await list.Content.ReadFromJsonAsync<GroupResponse[]>())!);

        using var configurableListRequest = ActingRequest.Get(prefix + "/list?operation=create", actor);
        using var configurableList = await client.SendAsync(configurableListRequest);
        Assert.Equal(HttpStatusCode.BadRequest, configurableList.StatusCode);

        using var createTargetsRequest = ActingRequest.Get(prefix + "/create-targets?take=20", actor);
        using var createTargets = await client.SendAsync(createTargetsRequest);
        Assert.Equal(HttpStatusCode.OK, createTargets.StatusCode);
        Assert.NotEmpty((await createTargets.Content.ReadFromJsonAsync<GroupResponse[]>())!);

        using var configurableTargetsRequest = ActingRequest.Get(prefix + "/create-targets?operation=read", actor);
        using var configurableTargets = await client.SendAsync(configurableTargetsRequest);
        Assert.Equal(HttpStatusCode.BadRequest, configurableTargets.StatusCode);
    }
}
