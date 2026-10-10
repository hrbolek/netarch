using System.Data.Common;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;
using SecurityLayerImpl = WebAppArchitecture.SecurityLayer.SecurityLayer;
using Xunit;

namespace Core.SecurityLayer.Tests;

/// <summary>
/// T14: SQL command-count regressions + explicitly opted-in large data scenario.
/// These are EF Core/provider-agnostic assertions; SQLite is only the local fixture.
/// Times are recorded for inspection, never enforced as flaky thresholds.
/// </summary>
public sealed class Stage4LargeAccessTests
{
    private static SecurityLayerImpl Layer(RbacFixture f, Guid user)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["x-ms-client-principal-id"] = user.ToString();
        var actor = new HeaderOrClaimActorProvider();
        return new SecurityLayerImpl(f.Db, new SecurityService(f.Db, actor),
            actor, new HttpContextAccessor { HttpContext = http });
    }

    [Fact]
    public async Task T14_SecureListAndCount_DoNotIssuePerEntityQueries()
    {
        var counter = new SqlCommandCounter();
        using var f = new RbacFixture(counter);
        f.Operation(f.ApplicantType, SecurityOperations.Read);
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        var ids = Enumerable.Range(0, 96).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var id in ids) f.AddGroup(id, f.Request, $"Item-{id:N}");
        f.Context.SaveChanges();
        await f.Sync();
        var secure = Layer(f, f.Student).Query<GroupModel>(SecurityOperations.Read);
        counter.Reset();
        var found = await secure.Where(x => ids.Contains(x.id)).Select(x => x.id).ToArrayAsync();
        Assert.Equal(ids.Length, found.Length);
        Assert.Equal(1, counter.Count); // No N+1 for secured list
        counter.Reset();
        Assert.Equal(ids.Length, await secure.CountAsync(x => ids.Contains(x.id)));
        Assert.Equal(1, counter.Count); // Count is evaluated by the database
    }

    [Fact]
    public void T14_EffectiveAccessAndRoleOperation_HaveQueryableIndexes()
    {
        using var f = new RbacFixture();
        var model = f.Context.Model;
        var accessIndexes = model.FindEntityType(typeof(EffectiveAccessModel))!
            .GetIndexes().Select(i => i.Properties.Select(p => p.Name).ToArray()).ToArray();
        Assert.Contains(accessIndexes, p => p.Length >= 2 && p[0] == "user_id" && p[1] == "rbacobject_id");
        Assert.Contains(accessIndexes, p => p.Length >= 2 && p[0] == "rbacobject_id" && p[1] == "user_id");
        var mappingIndexes = model.FindEntityType(typeof(RoleTypeOperationModel))!
            .GetIndexes().Select(i => i.Properties.Select(p => p.Name).ToArray()).ToArray();
        Assert.Contains(mappingIndexes, p => p.Length >= 2 && p[0] == "role_type_id" && p[1] == "operation_id");
    }

    [Fact]
    public async Task T14_LargePopulation_OptIn_MeasuresProjectionAndSecureQueries()
    {
        // Opt-in so ordinary dotnet test remains fast. To run:
        // STAGE4_STRESS=1 dotnet test ... --filter FullyQualifiedName~T14_LargePopulation
        if (Environment.GetEnvironmentVariable("STAGE4_STRESS") != "1") return;

        var counter = new SqlCommandCounter();
        using var f = new RbacFixture(counter);
        f.Operation(f.ApplicantType, SecurityOperations.Read);
        var users = Enumerable.Range(0, 1500).Select(_ => Guid.NewGuid()).ToArray();
        // Use a fixed set of groups, avoiding a fixture dominated by EF tracking
        // instead of realistic authorizations. 1500 users x multiple inherited scopes
        // produces thousands of EffectiveAccess rows.
        var targets = Enumerable.Range(0, 24).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var g in targets) f.AddGroup(g, f.Request, $"Resource-{g:N}");
        foreach (var u in users)
        {
            f.Context.Users.Add(new UserModel(u, $"Load-{u:N}", null, null,
                f.Root, f.Now, f.Now, f.Student, f.Student));
            f.Context.Roles.Add(new RoleModel(Guid.NewGuid(), f.Request, u, f.ApplicantType,
                false, f.Now.AddDays(-1), null, null, f.Request, f.Now, f.Now, f.Student, f.Student));
        }
        f.Context.SaveChanges();
        var watch = Stopwatch.StartNew();
        counter.Reset();
        await f.Sync();
        var rebuildCount = counter.Count;
        var rebuildTime = watch.Elapsed;
        var projected = await f.Context.EffectiveAccesses.AsNoTracking().CountAsync();
        Assert.True(projected >= users.Length, $"Expected >= {users.Length} EffectiveAccess, got {projected}");

        var query = Layer(f, users[0]).Query<GroupModel>(SecurityOperations.Read);
        counter.Reset(); watch.Restart();
        var actual = await query.Where(g => targets.Contains(g.id)).Select(g => g.id).ToArrayAsync();
        var queryTime = watch.Elapsed;
        Assert.Equal(targets.Length, actual.Length);
        Assert.Equal(1, counter.Count);
        counter.Reset(); watch.Restart();
        await f.Projection.SynchronizeRbacObjectAsync(targets[0]);
        var incrementalTime = watch.Elapsed;
        var incrementalCount = counter.Count;
        Assert.True(incrementalCount > 0);
        Console.WriteLine($"T14 stress: users={users.Length} projected={projected} " +
            $"rebuild={rebuildTime.TotalMilliseconds:F0}ms/{rebuildCount}cmd " +
            $"secureRead={queryTime.TotalMilliseconds:F0}ms/1cmd " +
            $"incremental={incrementalTime.TotalMilliseconds:F0}ms/{incrementalCount}cmd");
    }

    private sealed class SqlCommandCounter : DbCommandInterceptor
    {
        private int count;
        public int Count => System.Threading.Volatile.Read(ref count);
        public void Reset() => System.Threading.Interlocked.Exchange(ref count, 0);
        private void Increment() => System.Threading.Interlocked.Increment(ref count);
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result)
        { Increment(); return result; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        { Increment(); return ValueTask.FromResult(result); }
        public override InterceptionResult<object> ScalarExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<object> result)
        { Increment(); return result; }
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        { Increment(); return ValueTask.FromResult(result); }
        public override InterceptionResult<int> NonQueryExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result)
        { Increment(); return result; }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        { Increment(); return ValueTask.FromResult(result); }
    }
}
