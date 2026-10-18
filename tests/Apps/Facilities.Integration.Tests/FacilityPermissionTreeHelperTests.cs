using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Facilities;
using Xunit;

namespace Facilities.Integration.Tests;

public sealed class FacilityPermissionTreeHelperTests(FacilitiesPostgresFixture fixture)
    : IClassFixture<FacilitiesPostgresFixture>
{
    [Fact]
    public async Task CreateOwnedGroupRequiresCreateOnParentAndCreatesADistinctScope()
    {
        var parent = await fixture.CreateScopeAsync("Create-only permission parent");
        var actor = await fixture.CreateActorAsync("Create-only permission actor");
        await fixture.GrantAsync(actor, parent, SecurityOperations.Create);
        await fixture.RebuildAccessAsync();

        using var scope = new HelperScope(fixture, actor);
        var created = await scope.ExecuteAsync(ct => scope.Helper.CreateOwnedGroupAsync("Owned child", parent, ct));
        Assert.Equal(created.id, created.rbacobject_id);
        Assert.NotEqual(parent, created.id);
        Assert.Equal(parent, created.master_group_id);
    }

    [Fact]
    public async Task SourceLocksBlockOtherDatabaseWritersUntilTransactionEnds()
    {
        var source = await fixture.CreateScopeAsync("Locked permission source");
        using var scope = new HelperScope(fixture, fixture.AdministratorId);
        await scope.ExecuteAsync(async ct =>
        {
            await scope.Helper.LockAsync(ct);
            await using var writer = fixture.CreateContext();
            await using var transaction = await writer.Database.BeginTransactionAsync(ct);
            await writer.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '150ms'", ct);
            var error = await Assert.ThrowsAsync<PostgresException>(() =>
                writer.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE groups SET name = name WHERE id = {source}", ct));
            Assert.Equal(PostgresErrorCodes.LockNotAvailable, error.SqlState);
            return true;
        });

        await using var after = fixture.CreateContext();
        Assert.Equal(1, await after.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE groups SET name = name WHERE id = {source}"));
    }

    [Fact]
    public async Task PermissionTreeLockRequiresAnExistingSharedTransaction()
    {
        using var scope = new HelperScope(fixture, fixture.AdministratorId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Helper.LockAsync());
    }

    [Fact]
    public async Task FailedMoveCallbackRollsBackCopiedGroupsRolesMembershipsAndDenies()
    {
        var mover = await fixture.CreateActorAsync("Permission copy rollback mover");
        var source = await fixture.CreateScopeAsync("Permission copy rollback source");
        var destination = await fixture.CreateScopeAsync("Permission copy rollback destination");
        var typeScope = await fixture.CreateScopeAsync("Permission copy rollback facility type");
        var member = await fixture.CreateActorAsync("Permission copy rollback member");
        await fixture.GrantMoveAsync(mover, source);
        await fixture.GrantAsync(mover, destination, SecurityOperations.Create);
        var typeId = await fixture.SeedFacilityTypeAsync(typeScope);
        var facilityId = await fixture.SeedFacilityAsync(source, typeId, "Permission copy rollback facility");
        var facilityBefore = await fixture.ReadFacilityAsync(facilityId);
        var readRoleType = await fixture.CreateRoleTypeAsync(
            "Rollback copy reader " + Guid.NewGuid().ToString("N"), SecurityOperations.Read);
        var originalRoleId = await fixture.AssignRoleAsync(readRoleType, member, source);
        var originalMembershipId = await fixture.AddMembershipAsync(member, source);
        var originalDenyId = await fixture.AddDenyRuleAsync(readRoleType, source);
        await fixture.RebuildAccessAsync();

        var groupsBefore = await fixture.CountGroupsAsync();
        int rolesBefore;
        int membershipsBefore;
        int deniesBefore;
        await using (var context = fixture.CreateContext())
        {
            rolesBefore = await context.Roles.CountAsync();
            membershipsBefore = await context.Memberships.CountAsync();
            deniesBefore = await context.RbacDenyRules.CountAsync();
        }

        using var scope = new HelperScope(fixture, mover);
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ExecuteAsync(ct =>
            scope.Helper.CopyForMoveAsync<bool>(source, destination, async (groupMap, token) =>
            {
                await scope.Context.SaveChangesAsync(token);
                await scope.Context.Set<FacilityModel>().Where(x => x.id == facilityId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.rbacobject_id, groupMap[source]), token);
                throw new InvalidOperationException("Abort after permission copy.");
            }, ct)));
        Assert.Equal("Abort after permission copy.", failure.Message);

        Assert.Equal(groupsBefore, await fixture.CountGroupsAsync());
        var facilityAfter = await fixture.ReadFacilityAsync(facilityId);
        Assert.Equal(facilityBefore.rbacobject_id, facilityAfter.rbacobject_id);
        Assert.Equal(facilityBefore.path, facilityAfter.path);
        Assert.Equal(facilityBefore.updated_at, facilityAfter.updated_at);
        await using (var context = fixture.CreateContext())
        {
            Assert.Equal(rolesBefore, await context.Roles.CountAsync());
            Assert.Equal(membershipsBefore, await context.Memberships.CountAsync());
            Assert.Equal(deniesBefore, await context.RbacDenyRules.CountAsync());
            Assert.True(await context.Roles.AnyAsync(x => x.id == originalRoleId));
            Assert.True(await context.Memberships.AnyAsync(x => x.id == originalMembershipId));
            Assert.True(await context.RbacDenyRules.AnyAsync(x => x.id == originalDenyId));
        }
        Assert.False(await fixture.IsAllowedAsync(member, source, SecurityOperations.Read));
    }

    private sealed class HelperScope : IDisposable
    {
        private readonly ServiceProvider services;
        private readonly FacilitiesTestContext context;
        private readonly IAuthorizationLayer security;
        public FacilityPermissionTree Helper { get; }
        public FacilitiesTestContext Context => context;

        public HelperScope(FacilitiesPostgresFixture fixture, Guid actor)
        {
            context = fixture.CreateContext();
            services = new ServiceCollection()
                .AddSingleton<IBaseStore<GroupModel>>(new EfGroupStore(context))
                .BuildServiceProvider();
            var db = new DBLayer(context, services);
            var http = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("oid", actor.ToString())], "PermissionHelperTest"))
            };
            security = new AuthorizationLayer(db, new RbacAuthorizationService(db),
                new HeaderOrClaimActorProvider(), new HttpContextAccessor { HttpContext = http });
            Helper = new FacilityPermissionTree(security, db, context, new SecurityProjectionService(db));
        }

        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action)
            => security.ExecuteInTransactionAsync(async ct =>
            {
                await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(762306241)", ct);
                return await action(ct);
            });

        public void Dispose()
        {
            context.Dispose();
            services.Dispose();
        }
    }
}
