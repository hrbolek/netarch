using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Security;
using AuthorizationLayerImpl = WebAppArchitecture.AuthorizationLayer.AuthorizationLayer;
using Xunit;

namespace Core.AuthorizationLayer.Tests;

/// <summary>
/// Stage 1 extension. Assertions exercise the real SQLite projection and translated SQL.
/// Every case builds a fresh RBAC tree: no test depends on execution order.
/// </summary>
public sealed class AdditionalAuthorizationTests
{
    private static AuthorizationLayerImpl Secured(RbacFixture f, Guid? user)
    {
        var http = new DefaultHttpContext();
        if (user.HasValue) http.Request.Headers["x-ms-client-principal-id"] = user.Value.ToString();
        var actor = new HeaderOrClaimActorProvider();
        return new AuthorizationLayerImpl(f.Db, new RbacAuthorizationService(f.Db), actor,
            new HttpContextAccessor { HttpContext = http });
    }

    private static string[] Active(RbacFixture f)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return f.Context.EffectiveAccesses.AsNoTracking().AsEnumerable()
            .Where(x => x.valid_from_unix_ms <= now &&
                        (x.valid_to_unix_ms is null || x.valid_to_unix_ms >= now))
            .Select(x => $"{x.user_id}|{x.rbacobject_id}|{x.role_type_id}|{x.source_role_id}|{x.source_membership_id}|{x.source_access_binding_id}")
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    [Fact]
    public async Task T02_IndependentFaculty_RemainsIsolatedFromLocalDean()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Dean, f.Faculty, f.DeanType);
        await f.Sync();
        Assert.True(f.Has(f.Dean, f.Request, f.DeanType));
        Assert.False(f.Has(f.Dean, f.OtherFaculty, f.DeanType));
        Assert.False(f.Has(f.Dean, f.Root, f.DeanType));
    }

    [Fact]
    public async Task T02_NewGrandchild_ReceivesInheritedRoleOnTargetedSync()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Dean, f.Faculty, f.DeanType);
        await f.Sync();
        var id = Guid.NewGuid();
        f.AddGroup(id, f.SecretChild, "ThirdLevel");
        f.Context.SaveChanges();
        await f.Projection.SynchronizeGroupSubtreeAsync(f.Secret);
        Assert.True(f.Has(f.Dean, id, f.DeanType));
    }

    [Fact]
    public async Task T02_ReenableInheritance_RestoresAncestorRole()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Dean, f.Faculty, f.DeanType);
        var original = f.Context.Groups.Single(x => x.id == f.Secret);
        f.Context.Entry(original).CurrentValues.SetValues(original with { inherit_roles = false });
        f.Context.SaveChanges();
        await f.Sync();
        Assert.False(f.Has(f.Dean, f.SecretChild, f.DeanType));
        var current = f.Context.Groups.Single(x => x.id == f.Secret);
        f.Context.Entry(current).CurrentValues.SetValues(current with { inherit_roles = true });
        f.Context.SaveChanges();
        await f.Projection.SynchronizeGroupSubtreeAsync(f.Secret);
        Assert.True(f.Has(f.Dean, f.SecretChild, f.DeanType));
    }

    [Theory]
    [InlineData("read", true)]
    [InlineData("update", true)]
    [InlineData("create", false)]
    [InlineData("delete", false)]
    public async Task T03_OperationMatrix_IsExplicit(string operation, bool expected)
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "read");
        f.Operation(f.ApplicantType, "update");
        await f.Sync();
        var service = new RbacAuthorizationService(f.Db);
        Assert.Equal(expected, await service.IsAllowedAsync(f.Student, f.Visible, operation));
    }

    [Fact]
    public async Task T03_RemoveOnlyUpdate_DoesNotRevokeRead()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "read");
        f.Operation(f.ApplicantType, "update");
        await f.Sync();
        var operationId = f.Context.Operations.Single(x => x.name == "update").id;
        var link = f.Context.RoleTypeOperations.Single(x => x.role_type_id == f.ApplicantType && x.operation_id == operationId);
        f.Context.RoleTypeOperations.Remove(link);
        f.Context.SaveChanges();
        var security = new RbacAuthorizationService(f.Db);
        Assert.True(await security.IsAllowedAsync(f.Student, f.Visible, "read"));
        Assert.False(await security.IsAllowedAsync(f.Student, f.Visible, "update"));
    }

    [Fact]
    public async Task T06_RemoveBinding_RevokesMappedRoleButRetainsSourceRole()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        var bindingId = f.Bind(f.Request, f.ApplicantType, f.OtherFaculty, f.ReaderType);
        await f.Sync();
        Assert.True(f.Has(f.Student, f.OtherFaculty, f.ReaderType));
        f.Context.RbacAccessBindings.Remove(f.Context.RbacAccessBindings.Single(x => x.id == bindingId));
        f.Context.SaveChanges();
        await f.Projection.SynchronizeRbacObjectAsync(f.OtherFaculty);
        Assert.False(f.Has(f.Student, f.OtherFaculty, f.ReaderType));
        Assert.True(f.Has(f.Student, f.Visible, f.ApplicantType));
    }

    [Fact]
    public async Task T06_BindingRequiresSourceRole_WithoutSourceRoleItCannotGrant()
    {
        using var f = new RbacFixture();
        f.Bind(f.Request, f.ApplicantType, f.OtherFaculty, f.ReaderType);
        await f.Sync();
        Assert.False(f.Has(f.Student, f.OtherFaculty, f.ReaderType));
    }

    [Fact]
    public async Task T06_DenyOutsideBindingSource_DoesNotRemoveUnrelatedBinding()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Bind(f.Request, f.ApplicantType, f.OtherFaculty, f.ReaderType);
        f.Deny(f.Secret, f.ApplicantType);
        await f.Sync();
        Assert.True(f.Has(f.Student, f.OtherFaculty, f.ReaderType));
        Assert.False(f.Has(f.Student, f.Secret, f.ApplicantType));
    }

    [Fact]
    public async Task T07_TargetedAfterGrant_EqualsFullRebuild()
    {
        using var f = new RbacFixture();
        await f.Sync();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        await f.Projection.SynchronizeRoleImpactAsync(new[] { f.Request });
        var targeted = Active(f);
        await f.Sync();
        Assert.Equal(targeted, Active(f));
    }

    [Fact]
    public async Task T07_TargetedAfterRemoval_EqualsFullRebuild()
    {
        using var f = new RbacFixture();
        var roleId = f.AddRole(f.Student, f.Request, f.ApplicantType);
        await f.Sync();
        f.Context.Roles.Remove(f.Context.Roles.Single(x => x.id == roleId));
        f.Context.SaveChanges();
        await f.Projection.SynchronizeRoleImpactAsync(new[] { f.Request });
        var targeted = Active(f);
        await f.Sync();
        Assert.Equal(targeted, Active(f));
    }

    [Fact]
    public async Task T07_DuplicateSynchronizationTargets_HaveSameResultAsUniqueTargets()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        await f.Projection.SynchronizeRbacObjectsAsync(new[] { f.Secret, f.Visible, f.Secret, f.Visible });
        var first = Active(f);
        await f.Projection.SynchronizeRbacObjectsAsync(new[] { f.Secret, f.Visible });
        Assert.Equal(first, Active(f));
    }

    [Fact]
    public async Task T07_NewlyCreatedChild_FullAndTargetedProjectionsAgree()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Deny(f.Secret, f.ApplicantType);
        await f.Sync();
        var id = Guid.NewGuid();
        f.AddGroup(id, f.SecretChild, "NestedSecret");
        f.Context.SaveChanges();
        await f.Projection.SynchronizeRbacObjectAsync(id);
        var targeted = Active(f);
        await f.Sync();
        Assert.Equal(targeted, Active(f));
        Assert.False(f.Has(f.Student, id, f.ApplicantType));
    }

    [Fact]
    public async Task T08_DenyForDean_DoesNotAffectApplicant()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Dean, f.Faculty, f.DeanType);
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Deny(f.Secret, f.DeanType);
        await f.Sync();
        Assert.False(f.Has(f.Dean, f.SecretChild, f.DeanType));
        Assert.True(f.Has(f.Student, f.SecretChild, f.ApplicantType));
    }

    [Fact]
    public async Task T08_IndependentDenyRules_CanBeRemovedIndividually()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        var first = f.Deny(f.Secret, f.ApplicantType);
        f.Deny(f.Visible, f.ApplicantType);
        await f.Sync();
        f.Context.RbacDenyRules.Remove(f.Context.RbacDenyRules.Single(x => x.id == first));
        f.Context.SaveChanges();
        await f.Projection.SynchronizeGroupSubtreeAsync(f.Secret);
        Assert.True(f.Has(f.Student, f.Secret, f.ApplicantType));
        Assert.False(f.Has(f.Student, f.Visible, f.ApplicantType));
    }

    [Fact]
    public async Task T08_DenyAddedAfterGrant_TakesEffectOnTargetedReprojection()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        await f.Sync();
        Assert.True(f.Has(f.Student, f.SecretChild, f.ApplicantType));
        f.Deny(f.Secret, f.ApplicantType);
        await f.Projection.SynchronizeGroupSubtreeAsync(f.Secret);
        Assert.False(f.Has(f.Student, f.SecretChild, f.ApplicantType));
        Assert.True(f.Has(f.Student, f.Sibling, f.ApplicantType));
    }

    [Fact]
    public async Task T08_DenyOnParentDoesNotEraseIndependentRoleType()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.AddRole(f.Student, f.Request, f.ReaderType);
        f.Deny(f.Secret, f.ApplicantType);
        await f.Sync();
        Assert.False(f.Has(f.Student, f.SecretChild, f.ApplicantType));
        Assert.True(f.Has(f.Student, f.SecretChild, f.ReaderType));
    }

    [Fact]
    public async Task T09_WhereAndCount_DenyCannotBeBypassedByClientPredicate()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "read");
        f.Deny(f.Secret, f.ApplicantType);
        await f.Sync();
        var secured = Secured(f, f.Student);
        var count = secured.Query<GroupModel>("read")
            .Where(g => g.id == f.Secret || g.id == f.Visible).Count();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task T09_DeniedEntityIsNotExposedBySingleOrDefault()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "read");
        f.Deny(f.Secret, f.ApplicantType);
        await f.Sync();
        var secured = Secured(f, f.Student);
        Assert.Null(secured.Query<GroupModel>("read").SingleOrDefault(g => g.id == f.Secret));
        Assert.NotNull(secured.Query<GroupModel>("read").SingleOrDefault(g => g.id == f.Visible));
    }

    [Fact]
    public async Task T09_ExplicitlyUnauthorizedOperation_ReturnsNoRows()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "read");
        await f.Sync();
        Assert.Empty(Secured(f, f.Student).Query<GroupModel>("delete").ToArray());
    }

    [Fact]
    public async Task T09_AnotherUserCannotBorrowApplicantsAccess()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "read");
        await f.Sync();
        Assert.Contains(f.Request, Secured(f, f.Student).Query<GroupModel>("read").Select(x => x.id).ToArray());
        Assert.DoesNotContain(f.Request, Secured(f, f.Dean).Query<GroupModel>("read").Select(x => x.id).ToArray());
    }
}
