using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebAppArchitecture.DBLayer;
using Xunit;

namespace Core.SecurityLayer.Tests;

/// <summary>
/// T14 closing regressions: changing one security source must not rebuild the
/// unrelated materialized projection. No fixed wall-clock assertions and no
/// provider-specific SQL. Membership remains independent of Role.
/// </summary>
public sealed class Stage4IncrementalImpactTests
{
    private static string[] Snapshot(RbacFixture f, Guid user) => f.Context.EffectiveAccesses
        .AsNoTracking().Where(x => x.user_id == user)
        .Select(x => new { x.rbacobject_id, x.role_type_id, x.source_role_id,
                           x.source_group_id, x.valid_from_unix_ms, x.valid_to_unix_ms })
        .AsEnumerable().Select(x => $"{x.rbacobject_id}|{x.role_type_id}|{x.source_role_id}|{x.source_group_id}|{x.valid_from_unix_ms}|{x.valid_to_unix_ms}")
        .OrderBy(x => x, StringComparer.Ordinal).ToArray();

    [Fact]
    public async Task T14_AddRole_OnlyChangesItsAffectedSubtree()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Dean, f.OtherFaculty, f.DeanType);
        await f.Sync();
        var unrelated = Snapshot(f, f.Dean);
        var role = f.AddRole(f.Student, f.Request, f.ApplicantType);
        await f.Projection.SynchronizeRoleImpactAsync(new[] { f.Request });
        Assert.True(f.Has(f.Student, f.Visible, f.ApplicantType));
        Assert.False(f.Has(f.Student, f.OtherFaculty, f.ApplicantType));
        Assert.Equal(unrelated, Snapshot(f, f.Dean));
        Assert.All(f.Context.EffectiveAccesses.AsNoTracking().Where(x => x.source_role_id == role),
            x => Assert.Equal(f.Student, x.user_id));
    }

    [Fact]
    public async Task T14_RevokeRole_LeavesUnrelatedProjectionUnchanged()
    {
        using var f = new RbacFixture();
        var removedRole = f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.AddRole(f.Dean, f.OtherFaculty, f.DeanType);
        await f.Sync();
        var unrelated = Snapshot(f, f.Dean);
        f.Context.Roles.Remove(f.Context.Roles.Single(x => x.id == removedRole));
        f.Context.SaveChanges();
        await f.Projection.SynchronizeRoleImpactAsync(new[] { f.Request });
        Assert.False(f.Has(f.Student, f.Visible, f.ApplicantType));
        Assert.Equal(unrelated, Snapshot(f, f.Dean));
        Assert.All(f.Context.EffectiveAccesses.AsNoTracking().Where(x => x.source_role_id == removedRole),
            x => Assert.True(x.valid_to_unix_ms < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }

    [Fact]
    public async Task T14_RepeatedTargetSynchronization_PreservesAllProjectionRows()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.AddRole(f.Dean, f.OtherFaculty, f.DeanType);
        await f.Sync();
        var beforeStudent = Snapshot(f, f.Student);
        var beforeDean = Snapshot(f, f.Dean);
        await f.Projection.SynchronizeRbacObjectAsync(f.Visible);
        await f.Projection.SynchronizeRbacObjectAsync(f.Visible);
        Assert.Equal(beforeStudent, Snapshot(f, f.Student));
        Assert.Equal(beforeDean, Snapshot(f, f.Dean));
    }

    [Fact]
    public async Task T14_LocalDenySynchronizesOnlySelectedBranch()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.AddRole(f.Dean, f.OtherFaculty, f.DeanType);
        await f.Sync();
        var unrelated = Snapshot(f, f.Dean);
        f.Deny(f.Secret, f.ApplicantType, descendants: true);
        await f.Projection.SynchronizeGroupSubtreeAsync(f.Secret);
        Assert.False(f.Has(f.Student, f.Secret, f.ApplicantType));
        Assert.False(f.Has(f.Student, f.SecretChild, f.ApplicantType));
        Assert.True(f.Has(f.Student, f.Visible, f.ApplicantType));
        Assert.Equal(unrelated, Snapshot(f, f.Dean));
    }

    [Fact]
    public async Task T14_TargetedUpdate_CommandCountDoesNotGrowWithUnrelatedRoles()
    {
        var counter = new CountingInterceptor();
        using var f = new RbacFixture(counter);
        // 120 independent roles increase total projection size, but an update
        // for Request must not touch OtherFaculty's effective access rows.
        for (var i = 0; i < 120; i++)
        {
            var userId = Guid.NewGuid();
            f.Context.Users.Add(new UserModel(userId, $"Independent-{i}", null, null,
                f.Root, f.Now, f.Now, f.Student, f.Student));
            f.Context.Roles.Add(new RoleModel(Guid.NewGuid(), f.OtherFaculty, userId,
                f.DeanType, false, f.Now.AddDays(-1), null, null,
                f.OtherFaculty, f.Now, f.Now, f.Student, f.Student));
        }
        f.Context.SaveChanges();
        await f.Sync();
        var unaffected = f.Context.EffectiveAccesses.AsNoTracking()
            .Count(x => x.rbacobject_id == f.OtherFaculty);
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        counter.Reset();
        await f.Projection.SynchronizeRoleImpactAsync(new[] { f.Request });
        var commands = counter.Count;
        Assert.InRange(commands, 1, 20);
        Assert.Equal(unaffected, f.Context.EffectiveAccesses.AsNoTracking()
            .Count(x => x.rbacobject_id == f.OtherFaculty));
        Assert.True(f.Has(f.Student, f.Visible, f.ApplicantType));
    }

    private sealed class CountingInterceptor : DbCommandInterceptor
    {
        private int count;
        public int Count => System.Threading.Volatile.Read(ref count);
        public void Reset() => System.Threading.Interlocked.Exchange(ref count, 0);
        private void Bump() => System.Threading.Interlocked.Increment(ref count);
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result)
        { Bump(); return result; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        { Bump(); return ValueTask.FromResult(result); }
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result)
        { Bump(); return result; }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        { Bump(); return ValueTask.FromResult(result); }
        public override InterceptionResult<object> ScalarExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<object> result)
        { Bump(); return result; }
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        { Bump(); return ValueTask.FromResult(result); }
    }
}
