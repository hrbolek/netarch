using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;
using AuthorizationLayerImpl = WebAppArchitecture.AuthorizationLayer.AuthorizationLayer;
using Xunit;

namespace Core.AuthorizationLayer.Tests;

/// <summary>
/// Stage 2: T04 membership timing; T05 deputy roles; T10 secured writes;
/// T12 transactional guarantees. All cases use relational SQLite via RbacFixture.
/// </summary>
public sealed class Stage2LifecycleTests
{
    private static AuthorizationLayerImpl Layer(RbacFixture f, Guid user)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["x-ms-client-principal-id"] = user.ToString();
        var actor = new HeaderOrClaimActorProvider();
        return new AuthorizationLayerImpl(f.Db, new RbacAuthorizationService(f.Db), actor,
            new HttpContextAccessor { HttpContext = http });
    }

    private static Guid AddMembership(RbacFixture f, Guid group, Guid user, DateTimeOffset from, DateTimeOffset? to)
    {
        var id = Guid.NewGuid();
        f.Context.Memberships.Add(new MembershipModel(id, group, user, from, to,
            group, f.Now, f.Now, f.Student, f.Student));
        f.Context.SaveChanges();
        return id;
    }

    // T04 security contract: Membership describes group membership only.
    // It NEVER grants, revokes, limits, or changes a Role or its EffectiveAccess.
    // These tests intentionally compare projected access before and after membership changes.
    private static string[] ProjectionSnapshot(RbacFixture f) => f.Context.EffectiveAccesses
        .AsNoTracking()
        .Select(x => new
        {
            x.user_id, x.rbacobject_id, x.role_type_id, x.source_role_id,
            x.source_membership_id, x.source_access_binding_id,
            x.valid_from_unix_ms, x.valid_to_unix_ms
        })
        .ToArray()
        .Select(x => $"{x.user_id:D}|{x.rbacobject_id:D}|{x.role_type_id:D}|" +
                     $"{x.source_role_id:D}|{x.source_membership_id:D}|" +
                     $"{x.source_access_binding_id:D}|{x.valid_from_unix_ms}|{x.valid_to_unix_ms}")
        .OrderBy(x => x, StringComparer.Ordinal)
        .ToArray();

    [Fact] public async Task T04_ActiveMembership_DoesNotChangeRoleProjection()
    {
        using var f = new RbacFixture();
        var role = f.AddRole(f.Dean, f.Faculty, f.DeanType);
        await f.Sync();
        var before = ProjectionSnapshot(f);
        AddMembership(f, f.Request, f.Student, f.Now.AddDays(-1), null);
        await f.Sync();
        Assert.Equal(before, ProjectionSnapshot(f));
        Assert.True(f.Has(f.Dean, f.Request, f.DeanType));
        Assert.Equal(role, f.Context.Roles.AsNoTracking().Single(x => x.id == role).id);
    }

    [Fact] public async Task T04_FutureMembership_DoesNotChangeRoleProjection()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Dean, f.Faculty, f.DeanType);
        await f.Sync(); var before = ProjectionSnapshot(f);
        AddMembership(f, f.Request, f.Student, f.Now.AddDays(3), null);
        await f.Sync();
        Assert.Equal(before, ProjectionSnapshot(f));
    }

    [Fact] public async Task T04_ExpiredMembership_DoesNotChangeRoleProjection()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Dean, f.Faculty, f.DeanType);
        await f.Sync(); var before = ProjectionSnapshot(f);
        AddMembership(f, f.Request, f.Student, f.Now.AddDays(-4), f.Now.AddDays(-1));
        await f.Sync();
        Assert.Equal(before, ProjectionSnapshot(f));
        Assert.True(f.Has(f.Dean, f.Request, f.DeanType));
    }

    [Fact] public async Task T04_MembershipOutsideRoleInterval_DoesNotReactivateExpiredRole()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Dean, f.Faculty, f.DeanType, f.Now.AddDays(-4), f.Now.AddDays(-3));
        await f.Sync(); var before = ProjectionSnapshot(f);
        AddMembership(f, f.Request, f.Student, f.Now.AddDays(-1), null);
        await f.Sync();
        Assert.Equal(before, ProjectionSnapshot(f));
        Assert.False(f.Has(f.Dean, f.Request, f.DeanType));
    }

    [Fact] public async Task T04_RemoveMembership_DoesNotRevokeRoleOrItsProjection()
    {
        using var f = new RbacFixture();
        var role = f.AddRole(f.Dean, f.Faculty, f.DeanType);
        await f.Sync(); var before = ProjectionSnapshot(f);
        var id = AddMembership(f, f.Request, f.Student, f.Now.AddDays(-1), null);
        await f.Sync();
        f.Context.Memberships.Remove(f.Context.Memberships.Single(x => x.id == id));
        f.Context.SaveChanges(); await f.Sync();
        Assert.Equal(before, ProjectionSnapshot(f));
        Assert.True(f.Has(f.Dean, f.Request, f.DeanType));
        Assert.True(f.Context.Roles.AsNoTracking().Any(x => x.id == role));
    }

    [Fact] public async Task T04_RemovingOneOfTwoMemberships_DoesNotChangeRoleProjection()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Dean, f.Faculty, f.DeanType);
        await f.Sync(); var before = ProjectionSnapshot(f);
        var first = AddMembership(f, f.Request, f.Student, f.Now.AddDays(-1), null);
        AddMembership(f, f.Visible, f.Student, f.Now.AddDays(-1), null);
        await f.Sync();
        f.Context.Memberships.Remove(f.Context.Memberships.Single(x => x.id == first));
        f.Context.SaveChanges(); await f.Sync();
        Assert.Equal(before, ProjectionSnapshot(f));
        Assert.True(f.Has(f.Dean, f.Request, f.DeanType));
    }

    private static Guid Deputy(RbacFixture f, Guid user, Guid group, Guid type,
        DateTimeOffset from, DateTimeOffset? until, Guid? delegatedBy)
    {
        var id = Guid.NewGuid();
        f.Context.Roles.Add(new RoleModel(id, group, user, type, true, from, until,
            delegatedBy, group, f.Now, f.Now, f.Student, f.Student));
        f.Context.SaveChanges(); return id;
    }

    [Fact] public async Task T05_DeputyRole_IsProjectedForAssignedUser()
    {
        using var f = new RbacFixture();
        var source = f.AddRole(f.Dean, f.Faculty, f.DeanType);
        Deputy(f, f.Student, f.Faculty, f.DeanType, f.Now.AddDays(-1), null, source);
        await f.Sync();
        Assert.True(f.Has(f.Student, f.Request, f.DeanType));
        Assert.True(f.Has(f.Dean, f.Request, f.DeanType));
    }

    [Fact] public async Task T05_DeputyRole_FutureValidityDoesNotGiveAccess()
    {
        using var f = new RbacFixture();
        Deputy(f, f.Student, f.Faculty, f.DeanType, f.Now.AddDays(1), null, null);
        await f.Sync(); Assert.False(f.Has(f.Student, f.Request, f.DeanType));
    }

    [Fact] public async Task T05_DeputyRole_ExpiredValidityDoesNotGiveAccess()
    {
        using var f = new RbacFixture();
        Deputy(f, f.Student, f.Faculty, f.DeanType, f.Now.AddDays(-3), f.Now.AddDays(-1), null);
        await f.Sync(); Assert.False(f.Has(f.Student, f.Request, f.DeanType));
    }

    [Fact] public async Task T05_RemoveDeputyRole_RevokesOnlyDeputy()
    {
        using var f = new RbacFixture(); var original = f.AddRole(f.Dean, f.Faculty, f.DeanType);
        var deputy = Deputy(f, f.Student, f.Faculty, f.DeanType, f.Now.AddDays(-1), null, original);
        await f.Sync();
        f.Context.Roles.Remove(f.Context.Roles.Single(x => x.id == deputy));
        f.Context.SaveChanges(); await f.Sync();
        Assert.False(f.Has(f.Student, f.Request, f.DeanType));
        Assert.True(f.Has(f.Dean, f.Request, f.DeanType));
    }

    [Fact] public async Task T05_DeputyRole_DenyBlocksSensitiveSubtree()
    {
        using var f = new RbacFixture();
        Deputy(f, f.Student, f.Request, f.DeanType, f.Now.AddDays(-1), null, null);
        f.Deny(f.Secret, f.DeanType);
        await f.Sync();
        Assert.True(f.Has(f.Student, f.Visible, f.DeanType));
        Assert.False(f.Has(f.Student, f.SecretChild, f.DeanType));
    }

    [Fact] public async Task T10_ReadPermission_DoesNotPermitUpdate()
    {
        using var f = new RbacFixture(); f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "read"); await f.Sync();
        var entity = f.Context.Groups.AsNoTracking().Single(x => x.id == f.Visible);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Layer(f, f.Student).StageUpdateAsync(entity with { Name = "modified" }));
        Assert.Equal("Visible", f.Context.Groups.AsNoTracking().Single(x => x.id == f.Visible).Name);
    }

    [Fact] public async Task T10_UpdatePermission_AllowsAuthorizedUpdate()
    {
        using var f = new RbacFixture(); f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "update"); await f.Sync();
        var entity = f.Context.Groups.AsNoTracking().Single(x => x.id == f.Visible);
        await Layer(f, f.Student).StageUpdateAsync(entity with { Name = "Changed" });
        await f.Db.SaveChangesAsync();
        Assert.Equal("Changed", f.Context.Groups.AsNoTracking().Single(x => x.id == f.Visible).Name);
    }

    [Fact] public async Task T10_ReadPermission_DoesNotPermitDelete()
    {
        using var f = new RbacFixture(); f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "read"); await f.Sync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Layer(f, f.Student).StageDeleteAsync<GroupModel>(f.Visible));
        Assert.True(f.Context.Groups.AsNoTracking().Any(x => x.id == f.Visible));
    }

    [Fact] public async Task T10_OtherFacultyUpdate_IsRejectedDespiteLocalWrite()
    {
        using var f = new RbacFixture(); f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "update"); await f.Sync();
        var foreign = f.Context.Groups.AsNoTracking().Single(x => x.id == f.OtherFaculty);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Layer(f, f.Student).StageUpdateAsync(foreign with { Name = "Hijacked" }));
    }

    [Fact] public async Task T12_RollbackAfterAddingRole_LeavesNoPersistentRole()
    {
        using var f = new RbacFixture(); var id = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.ExecuteInTransactionAsync(async ct =>
        {
            await f.Db.StageCreateAsync(new RoleModel(id, f.Request, f.Student, f.ApplicantType, false,
                f.Now.AddDays(-1), null, null, f.Request, f.Now, f.Now, f.Student, f.Student), ct);
            await f.Db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Intentional rollback");
        }));
        f.Context.ChangeTracker.Clear();
        Assert.False(f.Context.Roles.AsNoTracking().Any(x => x.id == id));
    }

    [Fact] public async Task T12_RollbackAfterDenyRule_LeavesNoPersistentDeny()
    {
        using var f = new RbacFixture(); var id = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.ExecuteInTransactionAsync(async ct =>
        {
            await f.Db.StageCreateAsync(new RbacDenyRuleModel(id, f.Secret, f.ApplicantType, true,
                f.Secret, f.Now, f.Now, f.Student, f.Student), ct);
            await f.Db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Intentional rollback");
        }));
        f.Context.ChangeTracker.Clear();
        Assert.False(f.Context.RbacDenyRules.AsNoTracking().Any(x => x.id == id));
    }

    [Fact] public async Task T12_RepeatedProjectionSync_IsStable()
    {
        using var f = new RbacFixture(); f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Deny(f.Secret, f.ApplicantType); await f.Sync();
        var before = f.Context.EffectiveAccesses.AsNoTracking().Count();
        await f.Projection.SynchronizeGroupSubtreeAsync(f.Request);
        await f.Projection.SynchronizeGroupSubtreeAsync(f.Request);
        Assert.Equal(before, f.Context.EffectiveAccesses.AsNoTracking().Count());
        Assert.False(f.Has(f.Student, f.Secret, f.ApplicantType));
    }
}
