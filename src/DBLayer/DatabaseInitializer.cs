using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext db,
        string? systemDataSource = null,
        bool importSystemData = true,
        CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        // WAL persists in the SQLite database and allows readers to continue while
        // a writer transaction is active. Foreign keys are also enabled on the
        // initialization connection; normal runtime connections enable them via
        // the connection string configured by Host.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;", cancellationToken);

        var now = DateTimeOffset.UtcNow;

        await EnsureTechnicalRootAsync(db, now, cancellationToken);

        // Import only into a database which does not yet contain a real group tree.
        // This keeps application restarts cheap and makes the import idempotent.
        var hasImportedGroups = await db.Groups.AnyAsync(
            x => x.id != RbacWellKnown.RootGroupId,
            cancellationToken);

        if (importSystemData && !hasImportedGroups)
        {
            var source = string.IsNullOrWhiteSpace(systemDataSource)
                ? SystemDataImporter.DefaultSource
                : systemDataSource;

            await SystemDataImporter.ImportAsync(db, source, cancellationToken);
        }

        await EnsureBootstrapUsersAsync(db, now, cancellationToken);
        var operationNames = await EnsureOperationCatalogAsync(db, now, cancellationToken);
        await EnsureBootstrapRoleTypesAsync(db, now, cancellationToken);
        await EnsureDefaultRoleTypeOperationPolicyAsync(db, operationNames, cancellationToken);
        await EnsureBootstrapRolesAsync(db, now, cancellationToken);
    }

    private static async Task EnsureTechnicalRootAsync(
        ApplicationDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!await db.GroupTypes.AnyAsync(x => x.id == RbacWellKnown.RootGroupTypeId, cancellationToken))
        {
            db.GroupTypes.Add(new GroupTypeModel(
                RbacWellKnown.RootGroupTypeId, "Root group type", null,
                $"/{RbacWellKnown.RootGroupTypeId:D}/", RbacWellKnown.RootGroupId,
                now, now, RbacWellKnown.BootstrapAdminUserId, RbacWellKnown.BootstrapAdminUserId));
        }

        if (!await db.Groups.AnyAsync(x => x.id == RbacWellKnown.RootGroupId, cancellationToken))
        {
            db.Groups.Add(new GroupModel(
                RbacWellKnown.RootGroupId, "Root", RbacWellKnown.RootGroupTypeId, null,
                $"/{RbacWellKnown.RootGroupId:D}/", RbacWellKnown.RootGroupId,
                now, now, RbacWellKnown.BootstrapAdminUserId, RbacWellKnown.BootstrapAdminUserId));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureBootstrapUsersAsync(
        ApplicationDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var seed in FakeEntraSeedUsers.All.Append(new FakeEntraSeedUser(
                     RbacWellKnown.BootstrapAdminUserId, "Bootstrap Admin", "bootstrap@local", "Administrator")))
        {
            if (!await db.Users.AnyAsync(x => x.id == seed.id, cancellationToken))
            {
                db.Users.Add(new UserModel(seed.id, seed.Name, RbacWellKnown.RootGroupId, now, now,
                    RbacWellKnown.BootstrapAdminUserId, RbacWellKnown.BootstrapAdminUserId));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<string[]> EnsureOperationCatalogAsync(
        ApplicationDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var operationNames = new[]
        {
            RbacOperations.Create, RbacOperations.Read, RbacOperations.Update, RbacOperations.Delete,
            RbacOperations.List, RbacOperations.Vector, RbacOperations.ManageGroups,
            RbacOperations.ManageMemberships, RbacOperations.ManageRoles, RbacOperations.ManageSecurityObjects
        }.Select(x => x.Trim().ToLowerInvariant()).Distinct().ToArray();

        foreach (var operationName in operationNames)
        {
            if (!await db.Operations.AnyAsync(x => x.name == operationName, cancellationToken))
            {
                db.Operations.Add(new OperationModel(Guid.NewGuid(), operationName, now, now,
                    RbacWellKnown.BootstrapAdminUserId, RbacWellKnown.BootstrapAdminUserId));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return operationNames;
    }

    private static async Task EnsureBootstrapRoleTypesAsync(
        ApplicationDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var roleName in new[] { "Administrator", "Rector", "Dean" })
        {
            if (await db.RoleTypes.AnyAsync(x => x.name == roleName, cancellationToken))
                continue;

            db.RoleTypes.Add(new RoleTypeModel(Guid.NewGuid(), roleName, RbacWellKnown.RootGroupId,
                now, now, RbacWellKnown.BootstrapAdminUserId, RbacWellKnown.BootstrapAdminUserId));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// systemdata.hk2026.json contains RoleTypes, but it does not contain the
    /// new normalized RoleType -> Operation mapping used by this architecture.
    /// Therefore initialization applies a conservative bootstrap policy:
    /// administrator role types get all operations, all other role types get
    /// read + list. These are ordinary data rows and can later be changed via
    /// application administration without changing schema or importer code.
    /// </summary>
    private static async Task EnsureDefaultRoleTypeOperationPolicyAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<string> allOperationNames,
        CancellationToken cancellationToken)
    {
        var allOperations = await db.Operations
            .Where(x => allOperationNames.Contains(x.name))
            .ToArrayAsync(cancellationToken);
        var readOperations = allOperations
            .Where(x => x.name is RbacOperations.Read or RbacOperations.List)
            .ToArray();

        var roleTypes = await db.RoleTypes.ToArrayAsync(cancellationToken);
        foreach (var roleType in roleTypes)
        {
            if (await db.RoleTypeOperations.AnyAsync(x => x.role_type_id == roleType.id, cancellationToken))
                continue;

            var normalized = roleType.name.Trim().ToLowerInvariant();
            var selected = normalized is "administrator" or "administrátor"
                ? allOperations
                : readOperations;

            foreach (var operation in selected)
                db.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(), roleType.id, operation.id));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureBootstrapRolesAsync(
        ApplicationDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var seed in FakeEntraSeedUsers.All.Append(new FakeEntraSeedUser(
                     RbacWellKnown.BootstrapAdminUserId, "Bootstrap Admin", "bootstrap@local", "Administrator")))
        {
            var roleType = await db.RoleTypes.SingleAsync(x => x.name == seed.RootRoleType, cancellationToken);
            if (await db.Roles.AnyAsync(x => x.user_id == seed.id
                                           && x.group_id == RbacWellKnown.RootGroupId
                                           && x.role_type_id == roleType.id, cancellationToken))
                continue;

            db.Roles.Add(new RoleModel(Guid.NewGuid(), RbacWellKnown.RootGroupId, seed.id, roleType.id,
                false, now.AddYears(-1), null, null, RbacWellKnown.RootGroupId, now, now,
                RbacWellKnown.BootstrapAdminUserId, RbacWellKnown.BootstrapAdminUserId));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
