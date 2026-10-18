using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Events;
using WebAppArchitecture.Facilities;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace Facilities.Integration.Tests;

public sealed class FacilitiesSchemaBootstrapTests
{
    [Fact]
    public async Task BootstrapAddsFacilitiesToExistingCoreEventsDatabaseAndIsIdempotent()
    {
        using var database = new PostgresTestDatabase();
        var rootId = Guid.NewGuid();
        var rootTypeId = Guid.NewGuid();
        var administratorId = Guid.NewGuid();
        var eventTypeId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var existing = CreateCoreEventsContext(database.ConnectionString))
        {
            await existing.Database.EnsureCreatedAsync();
            existing.GroupTypes.Add(new GroupTypeModel(rootTypeId, "Existing Core organization", null,
                $"/{rootTypeId:D}/", rootId, now, now, administratorId, administratorId));
            existing.Groups.Add(new GroupModel(rootId, "Existing Core root", rootTypeId, null,
                $"/{rootId:D}/", true, rootId, now, now, administratorId, administratorId));
            existing.Users.Add(new UserModel(administratorId, "Existing Core administrator",
                null, null, rootId, now, now, administratorId, administratorId));
            existing.Set<EventTypeModel>().Add(new EventTypeModel
            {
                id = eventTypeId,
                Code = "EXISTING-EVENT-TYPE",
                Name = "Existing Events type",
                rbacobject_id = rootId,
                path = $"/{eventTypeId:D}/",
                created_at = now,
                updated_at = now,
                created_by = administratorId,
                updated_by = administratorId
            });
            existing.Set<EventModel>().Add(new EventModel
            {
                id = eventId,
                Name = "Existing Events row",
                EventTypeId = eventTypeId,
                StartsAt = now,
                EndsAt = now.AddHours(1),
                rbacobject_id = rootId,
                path = $"/{eventId:D}/",
                created_at = now,
                updated_at = now,
                created_by = administratorId,
                updated_by = administratorId
            });
            await existing.SaveChangesAsync();
        }

        await AssertTableExistsAsync(database.ConnectionString, "groups");
        await AssertTableExistsAsync(database.ConnectionString, "events");
        await AssertTableDoesNotExistAsync(database.ConnectionString, "facilities");

        await using (var composed = CreateFacilitiesContext(database.ConnectionString))
            await FacilitiesSchemaBootstrap.EnsureAsync(composed);

        await AssertTableExistsAsync(database.ConnectionString, "facilities");
        await AssertTableExistsAsync(database.ConnectionString, "facility_types");
        await AssertTableExistsAsync(database.ConnectionString, "facility_reservations");
        await AssertForeignKeyExistsAsync(database.ConnectionString,
            "facilities", "rbacobject_id", "groups", "id");
        await AssertForeignKeyExistsAsync(database.ConnectionString,
            "facility_reservations", "EventId", "events", "id");
        await AssertOperationProvisionedWithoutGrantAsync(database.ConnectionString);

        await using (var composed = CreateFacilitiesContext(database.ConnectionString))
            await FacilitiesSchemaBootstrap.EnsureAsync(composed);

        await AssertOperationProvisionedWithoutGrantAsync(database.ConnectionString);
        await AssertTableExistsAsync(database.ConnectionString, "facility_reservations");
        await using var verify = CreateFacilitiesContext(database.ConnectionString);
        Assert.Equal("Existing Core root", (await verify.Groups.SingleAsync(x => x.id == rootId)).Name);
        Assert.Equal("Existing Events row", (await verify.Set<EventModel>().SingleAsync(x => x.id == eventId)).Name);
        Assert.Empty(await verify.Set<FacilityModel>().ToListAsync());
    }

    private static CoreEventsTestContext CreateCoreEventsContext(string connectionString)
        => new(new DbContextOptionsBuilder<CoreEventsTestContext>()
            .UseNpgsql(connectionString).Options);

    private static FacilitiesTestContext CreateFacilitiesContext(string connectionString)
        => new(new DbContextOptionsBuilder<FacilitiesTestContext>()
            .UseNpgsql(connectionString).Options);

    private static async Task AssertTableExistsAsync(string connectionString, string table)
    {
        Assert.True(await QueryBoolAsync(connectionString,
            "SELECT to_regclass(format('public.%I', @table)) IS NOT NULL", table));
    }

    private static async Task AssertTableDoesNotExistAsync(string connectionString, string table)
    {
        Assert.False(await QueryBoolAsync(connectionString,
            "SELECT to_regclass(format('public.%I', @table)) IS NOT NULL", table));
    }

    private static async Task AssertForeignKeyExistsAsync(
        string connectionString, string table, string column, string referencedTable, string referencedColumn)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_constraint constraint_row
                JOIN pg_class table_row ON table_row.oid = constraint_row.conrelid
                JOIN pg_class referenced_row ON referenced_row.oid = constraint_row.confrelid
                JOIN pg_attribute column_row
                  ON column_row.attrelid = table_row.oid
                 AND column_row.attnum = constraint_row.conkey[1]
                JOIN pg_attribute referenced_column
                  ON referenced_column.attrelid = referenced_row.oid
                 AND referenced_column.attnum = constraint_row.confkey[1]
                WHERE constraint_row.contype = 'f'
                  AND table_row.relname = @table
                  AND column_row.attname = @column
                  AND referenced_row.relname = @referenced_table
                  AND referenced_column.attname = @referenced_column
                  AND constraint_row.confdeltype = 'r')
            """, connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        command.Parameters.AddWithValue("referenced_table", referencedTable);
        command.Parameters.AddWithValue("referenced_column", referencedColumn);
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    private static async Task AssertOperationProvisionedWithoutGrantAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT (SELECT count(*) = 1 FROM operations
                    WHERE id = @operation_id AND lower(name) = 'move')
               AND NOT EXISTS (
                    SELECT 1 FROM role_type_operations WHERE operation_id = @operation_id)
            """, connection);
        command.Parameters.AddWithValue("operation_id", FacilitiesSchemaBootstrap.MoveOperationId);
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    private static async Task<bool> QueryBoolAsync(string connectionString, string sql, string table)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("table", table);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}

public sealed class CoreEventsTestContext(DbContextOptions<CoreEventsTestContext> options) : CoreDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureEvents();
    }
}
