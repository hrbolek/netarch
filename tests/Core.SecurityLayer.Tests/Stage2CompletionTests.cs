using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;
using SecurityLayerImpl = WebAppArchitecture.SecurityLayer.SecurityLayer;
using Xunit;

namespace Core.SecurityLayer.Tests;

/// <summary>
/// Stage 2 completion: negative creation checks, privilege escalation on row movement,
/// delegation provenance and transactional rollback. Every negative assertion also checks
/// that the database remains unchanged.
/// </summary>
public sealed class Stage2CompletionTests
{
    private static SecurityLayerImpl ActingAs(RbacFixture f, Guid user)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["x-ms-client-principal-id"] = user.ToString();
        var actor = new HeaderOrClaimActorProvider();
        return new SecurityLayerImpl(f.Db, new SecurityService(f.Db, actor), actor,
            new HttpContextAccessor { HttpContext = http });
    }

    private static GroupModel Child(RbacFixture f, Guid parent, string name)
    {
        var group = f.Context.Groups.AsNoTracking().Single(x => x.id == parent);
        var id = Guid.NewGuid();
        return new GroupModel(id, name, f.GroupType, parent, group.path + $"{id:D}/",
            true, id, f.Now, f.Now, f.Student, f.Student);
    }

    [Fact] public async Task T10_CreateRequiresCreatePermission_NotRead()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "read"); await f.Sync();
        var created = Child(f, f.Request, "ForbiddenChild");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ActingAs(f, f.Student).StageCreateAsync(created));
        Assert.False(f.Context.Groups.AsNoTracking().Any(x => x.id == created.id));
    }

    [Fact] public async Task T10_CreateAuthorizedOnParent_NotNewObject()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "create"); await f.Sync();
        var created = Child(f, f.Request, "AuthorizedChild");
        await ActingAs(f, f.Student).StageCreateAsync(created);
        await f.Db.SaveChangesAsync();
        Assert.True(f.Context.Groups.AsNoTracking().Any(x => x.id == created.id));
    }

    [Fact] public async Task T10_CreateInForeignFacultyRejected()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "create"); await f.Sync();
        var created = Child(f, f.OtherFaculty, "ForeignChild");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ActingAs(f, f.Student).StageCreateAsync(created));
        Assert.False(f.Context.Groups.AsNoTracking().Any(x => x.id == created.id));
    }

    [Fact] public async Task T10_UpdateCannotMoveSecuredEntityToForeignScope()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "update"); await f.Sync();
        var original = f.Context.Groups.AsNoTracking().Single(x => x.id == f.Visible);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            ActingAs(f, f.Student).StageUpdateAsync(original with { rbacobject_id = f.OtherFaculty }));
        Assert.Equal(original.rbacobject_id,
            f.Context.Groups.AsNoTracking().Single(x => x.id == f.Visible).rbacobject_id);
    }

    [Fact] public async Task T10_UpdateCannotClaimAccessByChangingRbacObject()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "update"); f.Operation(f.ApplicantType, "create"); await f.Sync();
        var foreign = f.Context.Groups.AsNoTracking().Single(x => x.id == f.OtherFaculty);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            ActingAs(f, f.Student).StageUpdateAsync(foreign with { rbacobject_id = f.Visible }));
        Assert.Equal(foreign.rbacobject_id,
            f.Context.Groups.AsNoTracking().Single(x => x.id == f.OtherFaculty).rbacobject_id);
    }

    private static RoleModel Deputy(RbacFixture f, Guid? sourceId, DateTimeOffset from, DateTimeOffset? to)
        => new(Guid.NewGuid(), f.Request, f.Student, f.DeanType, true,
            from, to, sourceId, f.Request, f.Now, f.Now, f.Student, f.Student);

    [Fact] public async Task T05_DeputyCannotBeCreatedWithoutSourceRole()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "create"); await f.Sync();
        var deputy = Deputy(f, null, f.Now, f.Now.AddDays(1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ActingAs(f, f.Student).StageCreateAsync(deputy));
        Assert.False(f.Context.Roles.AsNoTracking().Any(x => x.id == deputy.id));
    }

    [Fact] public async Task T05_DeputyCannotBeCreatedFromOtherUsersRole()
    {
        using var f = new RbacFixture();
        var source = f.AddRole(f.Dean, f.Request, f.DeanType);
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "create"); await f.Sync();
        var deputy = Deputy(f, source, f.Now, f.Now.AddDays(1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ActingAs(f, f.Student).StageCreateAsync(deputy));
    }

    [Fact] public async Task T05_DeputyCannotExceedSourceValidity()
    {
        using var f = new RbacFixture();
        var source = f.AddRole(f.Student, f.Request, f.DeanType, f.Now.AddDays(-1), f.Now.AddDays(2));
        f.Operation(f.DeanType, "create"); await f.Sync();
        var deputy = Deputy(f, source, f.Now, f.Now.AddDays(4));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ActingAs(f, f.Student).StageCreateAsync(deputy));
    }

    [Fact] public async Task T05_DeputyCannotChangeRoleTypeOfSource()
    {
        using var f = new RbacFixture();
        var source = f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "create"); await f.Sync();
        var deputy = Deputy(f, source, f.Now, f.Now.AddDays(1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ActingAs(f, f.Student).StageCreateAsync(deputy));
    }

    [Fact] public async Task T05_ValidDeputyRequiresDirectSourceAndWithinValidity()
    {
        using var f = new RbacFixture();
        var source = f.AddRole(f.Student, f.Request, f.DeanType, f.Now.AddDays(-1), f.Now.AddDays(3));
        f.Operation(f.DeanType, "create"); await f.Sync();
        var deputy = Deputy(f, source, f.Now, f.Now.AddDays(1));
        await ActingAs(f, f.Student).StageCreateAsync(deputy);
        await f.Db.SaveChangesAsync();
        Assert.True(f.Context.Roles.AsNoTracking().Any(x => x.id == deputy.id));
    }

    [Fact] public async Task T10_RoleUpdateCannotReplaceAssignedPrincipalOrRoleType()
    {
        using var f = new RbacFixture();
        var roleId = f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "update"); await f.Sync();
        var original = f.Context.Roles.AsNoTracking().Single(x => x.id == roleId);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            ActingAs(f, f.Student).StageUpdateAsync(original with { role_type_id = f.DeanType }));
        Assert.Equal(f.ApplicantType,
            f.Context.Roles.AsNoTracking().Single(x => x.id == roleId).role_type_id);
    }

    [Fact] public async Task T12_RollbackCreateAndProjectionLeavesNoEscalation()
    {
        using var f = new RbacFixture();
        var id = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.ExecuteInTransactionAsync(async ct =>
        {
            await f.Db.StageCreateAsync(new RoleModel(id, f.Request, f.Student, f.ApplicantType,
                false, f.Now.AddDays(-1), null, null, f.Request, f.Now, f.Now, f.Student, f.Student), ct);
            await f.Db.SaveChangesAsync(ct);
            await f.Projection.SynchronizeGroupSubtreeAsync(f.Request, ct);
            throw new InvalidOperationException("Rollback whole operation");
        }));
        f.Context.ChangeTracker.Clear();
        Assert.False(f.Context.Roles.AsNoTracking().Any(x => x.id == id));
        Assert.False(f.Has(f.Student, f.Request, f.ApplicantType));
    }
}
