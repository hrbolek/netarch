using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

/// <summary>
/// Typ Database Initializer ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext db,
        string? systemDataSource = null,
        bool importSystemData = true,
        CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

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

        await EnsureCreationScopesAsync(db, now, cancellationToken);
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
                $"/{RbacWellKnown.RootGroupId:D}/", true, RbacWellKnown.RootGroupId,
                now, now, RbacWellKnown.BootstrapAdminUserId, RbacWellKnown.BootstrapAdminUserId));
        }

        await db.SaveChangesAsync(cancellationToken);
    }


    private static async Task EnsureCreationScopesAsync(
        ApplicationDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var root = await db.Groups.SingleAsync(x => x.id == RbacWellKnown.RootGroupId, cancellationToken);
        var scopes = new (Guid Id, string Name)[]
        {
            (RbacWellKnown.FormDefinitionCreationScopeId, "Create Form Definitions"),
            (RbacWellKnown.WorkflowDefinitionCreationScopeId, "Create Workflow Definitions"),
            (RbacWellKnown.ProcessDefinitionCreationScopeId, "Create Process Definitions"),
            (RbacWellKnown.FormNodeTypeCreationScopeId, "Create Form Node Types")
        };

        foreach (var scope in scopes)
        {
            if (await db.Groups.AnyAsync(x => x.id == scope.Id, cancellationToken))
                continue;

            db.Groups.Add(new GroupModel(
                scope.Id, scope.Name, RbacWellKnown.RootGroupTypeId, RbacWellKnown.RootGroupId,
                $"{root.path}{scope.Id:D}/", true, scope.Id,
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
                db.Users.Add(new UserModel(
                    seed.id, seed.Name, seed.Email, null, RbacWellKnown.RootGroupId, now, now,
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
            RbacOperations.ManageMemberships, RbacOperations.ManageRoles, RbacOperations.ManageSecurityObjects,
            RbacOperations.StartProcess
        }.Select(x => x.Trim().ToLowerInvariant()).Distinct().ToArray();

        // Operation ids became deterministic in Update 19 because runtime authorization
        // compares RoleTypeOperation.operation_id directly and no longer joins Operations.
        // Existing databases can still contain the same operation names under historical,
        // randomly generated ids. Reconcile them transactionally instead of inserting a
        // duplicate name (operations.name is unique) or requiring the database to be reset.
        foreach (var operationName in operationNames)
            await EnsureWellKnownOperationAsync(db, operationName, now, cancellationToken);

        return operationNames;
    }

    private static async Task EnsureWellKnownOperationAsync(
        ApplicationDbContext db,
        string operationName,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var operationId = RbacOperationIds.FromName(operationName);

        var byId = await db.Operations
            .SingleOrDefaultAsync(x => x.id == operationId, cancellationToken);
        var byName = await db.Operations
            .SingleOrDefaultAsync(x => x.name == operationName, cancellationToken);

        if (byId is not null)
        {
            if (!string.Equals(byId.name, operationName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Well-known operation id {operationId} is already used by operation '{byId.name}', " +
                    $"but '{operationName}' was expected.");

            return;
        }

        if (byName is null)
        {
            // PostgreSQL bootstrap: insert by the canonical ID.
            db.Operations.Add(new OperationModel(operationId, operationName, now, now,
                RbacWellKnown.BootstrapAdminUserId, RbacWellKnown.BootstrapAdminUserId));
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var oldOperationId = byName.id;
        var legacyName = $"__legacy__{operationName}_{oldOperationId:N}";

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Free the unique operation name first. We intentionally do this before
            // inserting the new row, while both changes remain invisible until commit.
            db.Entry(byName).Property(x => x.name).CurrentValue = legacyName;
            db.Entry(byName).Property(x => x.updated_at).CurrentValue = now;
            db.Entry(byName).Property(x => x.updated_by).CurrentValue = RbacWellKnown.BootstrapAdminUserId;
            await db.SaveChangesAsync(cancellationToken);

            var replacement = new OperationModel(operationId, operationName, byName.created_at, now,
                byName.created_by, RbacWellKnown.BootstrapAdminUserId);
            db.Operations.Add(replacement);
            await db.SaveChangesAsync(cancellationToken);

            var links = await db.RoleTypeOperations
                .Where(x => x.operation_id == oldOperationId)
                .ToArrayAsync(cancellationToken);

            foreach (var link in links)
                db.Entry(link).Property(x => x.operation_id).CurrentValue = operationId;

            await db.SaveChangesAsync(cancellationToken);

            db.Operations.Remove(byName);
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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
