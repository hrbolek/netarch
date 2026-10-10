using WebAppArchitecture.Tests.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;
using SecurityLayerImpl = WebAppArchitecture.SecurityLayer.SecurityLayer;
using Xunit;

namespace Core.SecurityLayer.Tests;

/// <summary>
/// Cross-context transaction contracts expressed only via EF Core and IDBLayer.
/// The test fixture chooses SQLite, but the assertions do not depend on SQLite
/// error codes, locking modes, PRAGMAs, SQL strings, or isolation-level defaults.
/// Run the same scenarios with PostgreSQL and SQL Server fixture adapters in CI.
/// </summary>
public sealed class Stage2ProviderIntegrationTests
{
    private sealed class FileDatabase : IDisposable
    {
        private readonly PostgresTestDatabase database = new();
        private readonly ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        private DbContextOptions<ApplicationDbContext> Options =>
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(database.ConnectionString)
                .Options;

        public FileDatabase()
        {
            using var context = Open();
            context.Database.EnsureCreated();
        }

        public ApplicationDbContext Open() => new(Options);
        public IDBLayer Layer(ApplicationDbContext context) => new DBLayer(context, services);
        public void Dispose()
        {
            services.Dispose();
            database.Dispose();
        }
    }

    private static OperationModel Item(string name)
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        return new OperationModel(Guid.NewGuid(), name, now, now, actor, actor);
    }

    [Fact]
    public async Task T12_SeparateContexts_CommitIsVisibleAfterTransaction()
    {
        using var database = new FileDatabase();
        using var writer = database.Open();
        using var reader = database.Open();
        var db = database.Layer(writer);
        var item = Item($"cross_context_commit_{Guid.NewGuid():N}");

        await db.ExecuteInTransactionAsync(async ct =>
        {
            Assert.True(db.HasActiveTransaction);
            await db.StageCreateAsync(item, ct);
            await db.SaveChangesAsync(ct);
        });

        Assert.False(db.HasActiveTransaction);
        Assert.True(await reader.Operations.AsNoTracking().AnyAsync(x => x.id == item.id));
    }

    [Fact]
    public async Task T12_SeparateContexts_RollbackIsNeverVisible()
    {
        using var database = new FileDatabase();
        using var writer = database.Open();
        using var reader = database.Open();
        var db = database.Layer(writer);
        var item = Item($"cross_context_rollback_{Guid.NewGuid():N}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.ExecuteInTransactionAsync(async ct =>
        {
            await db.StageCreateAsync(item, ct);
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Force rollback");
        }));

        Assert.False(db.HasActiveTransaction);
        Assert.False(await reader.Operations.AsNoTracking().AnyAsync(x => x.id == item.id));
    }

    [Fact]
    public async Task T12_SeparateContexts_SequentialWritersPreserveBothCommits()
    {
        using var database = new FileDatabase();
        using var first = database.Open();
        using var second = database.Open();
        using var observer = database.Open();
        var firstDb = database.Layer(first);
        var secondDb = database.Layer(second);
        var a = Item($"first_writer_{Guid.NewGuid():N}");
        var b = Item($"second_writer_{Guid.NewGuid():N}");

        // No provider-specific assumption about how two simultaneous writers
        // are queued or rejected. Serializability under true contention is a
        // distinct provider-specific stress test.
        await firstDb.ExecuteInTransactionAsync(async ct =>
        {
            await firstDb.StageCreateAsync(a, ct);
            await firstDb.SaveChangesAsync(ct);
        });
        await secondDb.ExecuteInTransactionAsync(async ct =>
        {
            await secondDb.StageCreateAsync(b, ct);
            await secondDb.SaveChangesAsync(ct);
        });

        Assert.Equal(2, await observer.Operations.AsNoTracking()
            .CountAsync(x => x.id == a.id || x.id == b.id));
    }

    [Fact]
    public async Task T10_CreateChildAndRebuildProjection_ChildBecomesReadable()
    {
        using var f = new RbacFixture();
        f.AddRole(f.Student, f.Request, f.ApplicantType);
        f.Operation(f.ApplicantType, "create");
        f.Operation(f.ApplicantType, "read");
        await f.Sync();

        var parent = f.Context.Groups.AsNoTracking().Single(x => x.id == f.Request);
        var id = Guid.NewGuid();
        var child = new GroupModel(id, "IntegrationChild", f.GroupType, f.Request,
            parent.path + $"{id:D}/", true, id, f.Now, f.Now, f.Student, f.Student);
        var http = new DefaultHttpContext();
        http.Request.Headers["x-ms-client-principal-id"] = f.Student.ToString();
        var actor = new HeaderOrClaimActorProvider();
        var security = new SecurityLayerImpl(f.Db, new SecurityService(f.Db, actor), actor,
            new HttpContextAccessor { HttpContext = http });

        await security.StageCreateAsync(child);
        await security.SaveChangesAsync();
        await f.Sync();

        Assert.True(f.Has(f.Student, id, f.ApplicantType));
        Assert.Equal(id, security.Query<GroupModel>().Single(x => x.id == id).id);
    }
}
