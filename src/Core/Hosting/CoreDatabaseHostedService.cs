using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.Hosting;

/// <summary>
/// Owns Core database lifecycle: schema/bootstrap/security projection on startup
/// and JSON database dump on graceful shutdown.
/// </summary>
public sealed class CoreDatabaseHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CoreDatabaseHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);
        // EnsureCreated does not add columns to an existing database. Run safe,
        // additive SQLite schema upgrades before any initializer/query touches the models.
        // This does not rebuild or delete historical Requests or Submissions.
        if (db.Database.IsSqlite())
        {
            await EnsureSqliteColumnAsync(db, "groups", "valid",
                "ALTER TABLE groups ADD COLUMN valid INTEGER NOT NULL DEFAULT 1", cancellationToken);
            await EnsureSqliteColumnAsync(db, "dp_process_definition_versions", "target_group_type_id",
                "ALTER TABLE dp_process_definition_versions ADD COLUMN target_group_type_id TEXT NULL", cancellationToken);
        }
        // EnsureCreated does not add tables to an existing SQLite database.
        // Keep developer databases and historical submissions intact when introducing
        // persistent Applicant policies and RBAC Deny rules.
        if (db.Database.IsSqlite())
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS rbac_deny_rules (
                    id TEXT NOT NULL PRIMARY KEY,
                    group_id TEXT NOT NULL,
                    role_type_id TEXT NOT NULL,
                    include_descendants INTEGER NOT NULL,
                    rbacobject_id TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    created_by TEXT NOT NULL,
                    updated_by TEXT NOT NULL
                );
                """, cancellationToken);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE UNIQUE INDEX IF NOT EXISTS IX_rbac_deny_rules_group_id_role_type_id
                ON rbac_deny_rules (group_id, role_type_id);
                """, cancellationToken);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS dp_form_node_applicant_access (
                    id TEXT NOT NULL PRIMARY KEY,
                    process_definition_version_id TEXT NOT NULL,
                    workflow_state_id TEXT NOT NULL,
                    form_node_id TEXT NOT NULL,
                    can_read INTEGER NOT NULL,
                    rbacobject_id TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    created_by TEXT NOT NULL,
                    updated_by TEXT NOT NULL
                );
                """, cancellationToken);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE UNIQUE INDEX IF NOT EXISTS IX_dp_form_node_applicant_access_process_definition_version_id_workflow_state_id_form_node_id
                ON dp_form_node_applicant_access (process_definition_version_id, workflow_state_id, form_node_id);
                """, cancellationToken);
        }

        var dumpPath = configuration["DatabaseDump:Path"]
            ?? Environment.GetEnvironmentVariable("DATABASE_DUMP_PATH")
            ?? "./data/dump.json";

        var dumpService = scope.ServiceProvider.GetRequiredService<IDatabaseDumpService>();
        var restoredFromDump = await dumpService.LoadIfExistsAsync(dumpPath, cancellationToken);

        if (restoredFromDump)
        {
            await DatabaseInitializer.InitializeAsync(
                db,
                systemDataSource: null,
                importSystemData: false);
        }
        else
        {
            var systemDataSource = configuration["SystemData:Source"]
                ?? Environment.GetEnvironmentVariable("SYSTEMDATA_SOURCE")
                ?? SystemDataImporter.DefaultSource;
            var importSystemData = configuration.GetValue("SystemData:Import", true);
            await DatabaseInitializer.InitializeAsync(db, systemDataSource, importSystemData);
        }

        var securityProjection = scope.ServiceProvider.GetRequiredService<ISecurityProjectionService>();
        var hasProjection = await db.EffectiveAccesses.AnyAsync(cancellationToken);
        var hasSecuritySources = await db.Roles.AnyAsync(cancellationToken)
            || await db.RbacAccessBindings.AnyAsync(cancellationToken);

        // Runtime mutations keep EffectiveAccess transactionally synchronized. A full
        // rebuild is therefore needed only when bootstrapping an empty projection
        // (fresh import/legacy database) or when explicitly requested for repair.
        var forceRebuild = configuration.GetValue("SecurityProjection:RebuildOnStartup", false);
        if (forceRebuild || (!hasProjection && hasSecuritySources))
        {
            logger.LogInformation("Rebuilding EffectiveAccess projection during startup bootstrap/repair.");
            await securityProjection.RebuildAllAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation("EffectiveAccess projection already present; full startup rebuild skipped.");
        }
    }

    /// <summary>
    /// Additive compatibility upgrade for pre-existing SQLite developer/test databases.
    /// Names and DDL are fixed internal constants; no user-controlled SQL is accepted.
    /// For PostgreSQL / SQL Server use provider migrations (stage 99).
    /// </summary>
    private static async Task EnsureSqliteColumnAsync(
        ApplicationDbContext db, string table, string column, string alterSql,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var openHere = connection.State != ConnectionState.Open;
        if (openHere)
            await connection.OpenAsync(cancellationToken);
        try
        {
            await using var inspect = connection.CreateCommand();
            inspect.CommandText = $"PRAGMA table_info(\"{table}\")";
            var tableExists = false;
            var columnExists = false;
            await using (var reader = await inspect.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    tableExists = true;
                    if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                        columnExists = true;
                }
            }
            if (!tableExists)
                throw new InvalidOperationException($"Expected SQLite table '{table}' does not exist after EnsureCreated.");
            if (!columnExists)
            {
                await using var alter = connection.CreateCommand();
                alter.CommandText = alterSql;
                await alter.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (openHere)
                await connection.CloseAsync();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var path = configuration["DatabaseDump:Path"]
            ?? Environment.GetEnvironmentVariable("DATABASE_DUMP_PATH")
            ?? "./data/dump.json";

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dump = scope.ServiceProvider.GetRequiredService<IDatabaseDumpService>();
            await dump.SaveAsync(path, cancellationToken);
            logger.LogInformation("Database dump saved to {DumpPath}", path);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Database dump was cancelled during shutdown.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save database dump during shutdown.");
        }
    }
}
