using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Membership.Models;
using WebAppArchitecture.Membership.Services;
using WebAppArchitecture.User.Models;
using WebAppArchitecture.User.Services;
using Xunit;
using Authorization = WebAppArchitecture.AuthorizationLayer.AuthorizationLayer;

namespace Core.AuthorizationLayer.Tests;

public sealed class SecuredServiceBoundaryTests
{
    private static Authorization Security(RbacFixture fixture, Guid? actor)
    {
        var http = new DefaultHttpContext();
        // There is no resolver or HTTP server in this suite. Context supplies
        // identity only; the real authorization layer still decides access.
        if (actor is Guid id)
            http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", id.ToString())], "test"));
        http.Request.QueryString = new QueryString("?actorUserId=" + fixture.Dean);
        http.Request.Headers["X-Actor-UserId"] = fixture.Dean.ToString();
        if (actor is null)
            http.Request.Headers.Clear();
        return new Authorization(fixture.Db, new RbacAuthorizationService(fixture.Db),
            new HeaderOrClaimActorProvider(), new HttpContextAccessor { HttpContext = http });
    }

    private static async Task<(Guid Visible, Guid Hidden)> Seed(RbacFixture fixture, params string[] operations)
    {
        fixture.AddRole(fixture.Student, fixture.Request, fixture.ApplicantType);
        foreach (var operation in operations)
            fixture.Operation(fixture.ApplicantType, operation);
        var visible = Guid.NewGuid();
        var hidden = Guid.NewGuid();
        fixture.Context.Users.AddRange(
            new UserModel(visible, "visible", null, null, fixture.Visible, fixture.Now, fixture.Now, fixture.Dean, fixture.Dean),
            new UserModel(hidden, "hidden", null, null, fixture.OtherFaculty, fixture.Now, fixture.Now, fixture.Dean, fixture.Dean));
        await fixture.Context.SaveChangesAsync();
        await fixture.Sync();
        return (visible, hidden);
    }

    [Fact]
    public async Task RealServiceFiltersSingleReadsAndCollectionsWithoutResolvers()
    {
        using var fixture = new RbacFixture();
        var ids = await Seed(fixture, RbacOperations.Read, RbacOperations.List);
        var security = Security(fixture, fixture.Student);
        var service = new UserService(security, fixture.Db, fixture.Projection);

        Assert.Equal(fixture.Student, security.CurrentUserId);
        Assert.NotNull(await service.GetByIdAsync<UserResponse>(ids.Visible));
        Assert.Null(await service.GetByIdAsync<UserResponse>(ids.Hidden));
        Assert.Equal(new[] { ids.Visible }, (await service.GetAllAsync<UserResponse>()).Select(item => item.id));
        var page = await service.VectorPageAsync<UserResponse>(_ => true, RbacOperations.Read, new VectorQueryOptions(Take: 10));
        Assert.Equal(new[] { ids.Visible }, page.Items.Select(item => item.id));
        Assert.Equal(new[] { ids.Visible }, service.Where<UserResponse>(_ => true, RbacOperations.Read).Select(item => item.id));
        Assert.False(await new RbacAuthorizationService(fixture.Db)
            .IsAllowedAsync(fixture.Student, fixture.OtherFaculty, RbacOperations.Read));
    }

    [Fact]
    public async Task ReadOnlyActorCannotCreateUpdateOrDeleteAndNothingPersists()
    {
        using var fixture = new RbacFixture();
        var ids = await Seed(fixture, RbacOperations.Read);
        var security = Security(fixture, fixture.Student);
        var users = new UserService(security, fixture.Db, fixture.Projection);
        var memberships = new MembershipService(security, fixture.Db, fixture.Projection);
        var before = await users.GetByIdAsync<UserResponse>(ids.Visible);
        Assert.NotNull(before);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            memberships.CreateContainedAsync(new MembershipCreateContainedRequest(fixture.Request, ids.Visible, fixture.Now)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            users.UpdateAsync<UserUpdateRequest, UserResponse>(new(ids.Visible, before.updated_at, "denied")));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => users.DeleteAsync(ids.Visible));

        using var verify = fixture.CreateContext();
        Assert.Equal("visible", (await verify.Users.SingleAsync(item => item.id == ids.Visible)).Name);
        Assert.False(await verify.Memberships.AnyAsync(item => item.user_id == ids.Visible));
        Assert.False(fixture.Db.HasActiveTransaction);
        Assert.DoesNotContain(fixture.Context.ChangeTracker.Entries(), entry =>
            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
    }

    [Fact]
    public async Task AuthorizedServiceWritesUseTrustedAuditActorAndPersist()
    {
        using var fixture = new RbacFixture();
        var ids = await Seed(fixture, RbacOperations.Read, RbacOperations.Create, RbacOperations.Update, RbacOperations.Delete);
        var security = Security(fixture, fixture.Student);
        var users = new UserService(security, fixture.Db, fixture.Projection);
        var memberships = new MembershipService(security, fixture.Db, fixture.Projection);
        var original = (await users.GetByIdAsync<UserResponse>(ids.Visible))!;
        var updated = await users.UpdateAsync<UserUpdateRequest, UserResponse>(new(ids.Visible, original.updated_at, "permitted"));
        Assert.Equal(fixture.Student, updated.updated_by);
        Assert.Equal(original.created_by, updated.created_by);
        Assert.True(updated.updated_at > original.updated_at);

        var created = await memberships.CreateContainedAsync(new(fixture.Request, ids.Visible, fixture.Now));
        Assert.Equal(fixture.Student, created.created_by);
        Assert.Equal(fixture.Request, created.rbacobject_id);
        using (var verify = fixture.CreateContext())
        {
            Assert.Equal("permitted", (await verify.Users.SingleAsync(item => item.id == ids.Visible)).Name);
            Assert.True(await verify.Memberships.AnyAsync(item => item.id == created.id));
        }
        await memberships.DeleteAsync(created.id);
        using (var verify = fixture.CreateContext())
            Assert.False(await verify.Memberships.AnyAsync(item => item.id == created.id));
        Assert.False(fixture.Db.HasActiveTransaction);
    }

    [Fact]
    public async Task PostSaveProjectionFailureRollsBackEntireServiceCommand()
    {
        using var fixture = new RbacFixture();
        var ids = await Seed(fixture, RbacOperations.Read, RbacOperations.Create);
        var projection = new FailingProjection(fixture.Db);
        var service = new MembershipService(Security(fixture, fixture.Student), fixture.Db, projection);

        await Assert.ThrowsAsync<ProjectionFailure>(() =>
            service.CreateContainedAsync(new(fixture.Request, ids.Visible, fixture.Now)));
        Assert.True(projection.SawTransaction);
        Assert.False(fixture.Db.HasActiveTransaction);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
        using var verify = fixture.CreateContext();
        Assert.False(await verify.Memberships.AnyAsync(item => item.user_id == ids.Visible));
    }

    [Fact]
    public async Task NoActorCannotReadOrWriteEvenWithoutHttpResolver()
    {
        using var fixture = new RbacFixture();
        var ids = await Seed(fixture, RbacOperations.Read);
        var users = new UserService(Security(fixture, null), fixture.Db, fixture.Projection);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => users.GetByIdAsync<UserResponse>(ids.Visible));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => users.DeleteAsync(ids.Visible));
        using var verify = fixture.CreateContext();
        Assert.True(await verify.Users.AnyAsync(item => item.id == ids.Visible));
    }

    private sealed class ProjectionFailure : Exception;

    private sealed class FailingProjection(IDBLayer db) : ISecurityProjectionService
    {
        public bool SawTransaction { get; private set; }

        public Task SynchronizeRbacObjectAsync(Guid rbacObjectId, CancellationToken cancellationToken = default)
        {
            SawTransaction = db.HasActiveTransaction;
            throw new ProjectionFailure();
        }

        public Task RebuildAllAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Unexpected rebuild");
        public Task SynchronizeRbacObjectsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Unexpected synchronization");
        public Task SynchronizeRoleImpactAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Unexpected role synchronization");
        public Task SynchronizeGroupSubtreeAsync(Guid id, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Unexpected subtree synchronization");
    }
}
