using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Facilities;

/// <summary>
/// All Facilities mutations share this transaction lock, including compatibility edits,
/// so concurrent inserts cannot slip into a subtree after its authorization check.
/// PostgreSQL locks are database-wide and held until the actual transaction commits.
/// PostgreSQL is required for these transaction and concurrency guarantees.
/// </summary>
internal static class FacilitiesWriteBoundary
{
    internal const long TreeLock = 762306241;

    public static async Task<T> ExecuteAsync<T>(IAuthorizationLayer security, CoreDbContext db,
        Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        if (!db.Database.IsNpgsql())
            throw new NotSupportedException("Facilities writes require PostgreSQL.");
        return await security.ExecuteInTransactionAsync(async c =>
        {
            if (db.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel != System.Data.IsolationLevel.ReadCommitted)
                throw new InvalidOperationException("Facilities commands require a ReadCommitted PostgreSQL transaction.");
            await db.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_xact_lock({TreeLock})", c);
            return await action(c);
        }, ct);
    }

    public static async Task RequireScopeAsync(IAuthorizationLayer security, IDBLayer db, Guid scope, CancellationToken ct)
    {
        // A permitted but nonexistent UUID is not a valid creation scope.
        if (scope == Guid.Empty || !await db.Query<GroupModel>().AnyAsync(x => x.id == scope, ct))
            throw new UnauthorizedAccessException("The creation scope is unavailable.");
        await CreationScopeGuard.RequireRbacAsync(security, scope, "explicit", cancellationToken: ct);
    }

    public static void Token(DateTimeOffset expected, DateTimeOffset actual)
    {
        if (expected == default) throw new ArgumentException("updated_at is required.");
        if (expected != actual) throw new DbUpdateConcurrencyException("The entity has changed; reload before retrying.");
    }

    public static void Range(DateTimeOffset start, DateTimeOffset end)
    {
        if (start == default || end == default || start >= end)
            throw new ArgumentException("A nonempty interval with StartsAt < EndsAt is required.");
    }

    public static int Take(int? take) => Math.Clamp(take ?? 50, 1, 200);
    public static int Skip(int? skip) => Math.Max(0, skip ?? 0);
}
