using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.RoleType.Models;

public sealed record RoleTypeModel(
    Guid id,
    string name,
    string operations_csv,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record RoleTypeResponse(
    Guid id,
    string name,
    string[] operations,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDTOModel<RoleTypeModel, RoleTypeResponse>, ISecuredEntityDTOModel
{
    public static RoleTypeResponse FromModel(RoleTypeModel model)
        => new(
            model.id,
            model.name,
            SplitOps(model.operations_csv),
            model.rbac_id,
            model.created_at,
            model.updated_at,
            model.created_by,
            model.updated_by);

    public RoleTypeModel ToModel()
        => new(
            id,
            name,
            JoinOps(operations),
            rbac_id,
            created_at,
            updated_at,
            created_by,
            updated_by);

    private static string[] SplitOps(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string JoinOps(IEnumerable<string> values)
        => string.Join(',', values.Select(v => NormalizeOp(v)).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase));

    private static string NormalizeOp(string op)
        => op.Trim().ToLowerInvariant();
}

public sealed record RoleTypeCreateRequest(
    string name,
    string[] operations,
    Guid? rbac_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<RoleTypeModel>, ISecuredCreateRequest
{
    public RoleTypeModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        return new(
            Guid.NewGuid(),
            name,
            JoinOps(operations),
            rbac_id ?? RbacWellKnown.RootSecurityObjectId,
            created_at ?? now,
            updated_at ?? now,
            created_by ?? Guid.Empty,
            updated_by ?? Guid.Empty);
    }

    private static string JoinOps(IEnumerable<string> values)
        => string.Join(',', values.Select(v => v.Trim().ToLowerInvariant()).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase));
}

public sealed record RoleTypeUpdateRequest(
    Guid id,
    string name,
    string[] operations,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<RoleTypeModel>, IEntityDTOModel<RoleTypeModel, RoleTypeUpdateRequest>
{
    public static RoleTypeUpdateRequest FromModel(RoleTypeModel model)
        => new(
            model.id,
            model.name,
            model.operations_csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            model.updated_at,
            model.updated_by);

    public RoleTypeModel ApplyTo(RoleTypeModel model)
        => model with
        {
            name = name,
            operations_csv = string.Join(',', operations.Select(v => v.Trim().ToLowerInvariant()).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase)),
            updated_at = updated_at ?? DateTimeOffset.UtcNow,
            updated_by = updated_by ?? model.updated_by
        };

    public RoleTypeModel ToModel()
        => new(
            id,
            name,
            string.Join(',', operations),
            RbacWellKnown.RootSecurityObjectId,
            DateTimeOffset.UtcNow,
            updated_at ?? DateTimeOffset.UtcNow,
            Guid.Empty,
            updated_by ?? Guid.Empty);
}

public interface IRoleTypeStore : IBaseStore<RoleTypeModel>
{
}

public sealed class SqliteRoleTypeStore : IRoleTypeStore
{
    private readonly string _connectionString;

    public SqliteRoleTypeStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureDatabase();
    }

    public async Task<RoleTypeModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, operations_csv, rbac_id, created_at, updated_at, created_by, updated_by
            FROM rbac_role_types_catalog
            WHERE id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadModel(reader) : null;
    }

    public async Task<IReadOnlyCollection<RoleTypeModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, operations_csv, rbac_id, created_at, updated_at, created_by, updated_by
            FROM rbac_role_types_catalog;
            """;

        var items = new List<RoleTypeModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(ReadModel(reader));

        return items;
    }

    public async Task<IReadOnlyCollection<RoleTypeModel>> VectorAsync(Expression<Func<RoleTypeModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(predicate.Compile()).ToArray();
    }

    public async Task<RoleTypeModel> CreateAsync(RoleTypeModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO rbac_role_types_catalog
                (id, name, operations_csv, rbac_id, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $name, $operations_csv, $rbac_id, $created_at, $updated_at, $created_by, $updated_by);
            """;
        Bind(command, model);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await SyncRoleTypeMappingsAsync(connection, model, cancellationToken);
        return model;
    }

    public async Task<RoleTypeModel> UpdateAsync(RoleTypeModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE rbac_role_types_catalog
            SET name = $name,
                operations_csv = $operations_csv,
                rbac_id = $rbac_id,
                created_at = $created_at,
                updated_at = $updated_at,
                created_by = $created_by,
                updated_by = $updated_by
            WHERE id = $id;
            """;
        Bind(command, model);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
            throw new KeyNotFoundException($"Role type '{model.id}' was not found.");

        await SyncRoleTypeMappingsAsync(connection, model, cancellationToken);
        return model;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await GetByIdAsync(id, cancellationToken);
        if (current is null)
            return;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var deleteOps = connection.CreateCommand())
        {
            deleteOps.CommandText = "DELETE FROM rbac_role_type_operations WHERE role_type = $role_type;";
            deleteOps.Parameters.AddWithValue("$role_type", current.name);
            await deleteOps.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteRoleType = connection.CreateCommand())
        {
            deleteRoleType.CommandText = "DELETE FROM rbac_role_types WHERE name = $name;";
            deleteRoleType.Parameters.AddWithValue("$name", current.name);
            await deleteRoleType.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM rbac_role_types_catalog WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void EnsureDatabase()
    {
        EnsureDataDirectoryExists();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var create1 = connection.CreateCommand();
        create1.CommandText = """
            CREATE TABLE IF NOT EXISTS rbac_role_types (
                name TEXT PRIMARY KEY
            );

            CREATE TABLE IF NOT EXISTS rbac_role_type_operations (
                role_type TEXT NOT NULL,
                operation TEXT NOT NULL,
                PRIMARY KEY (role_type, operation)
            );

            CREATE TABLE IF NOT EXISTS rbac_role_types_catalog (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL UNIQUE,
                operations_csv TEXT NOT NULL,
                rbac_id TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                created_by TEXT NOT NULL,
                updated_by TEXT NOT NULL
            );
            """;
        create1.ExecuteNonQuery();
    }

    private static async Task SyncRoleTypeMappingsAsync(SqliteConnection connection, RoleTypeModel model, CancellationToken cancellationToken)
    {
        await using (var upsertRoleType = connection.CreateCommand())
        {
            upsertRoleType.CommandText = "INSERT OR IGNORE INTO rbac_role_types (name) VALUES ($name);";
            upsertRoleType.Parameters.AddWithValue("$name", model.name);
            await upsertRoleType.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var clear = connection.CreateCommand())
        {
            clear.CommandText = "DELETE FROM rbac_role_type_operations WHERE role_type = $role_type;";
            clear.Parameters.AddWithValue("$role_type", model.name);
            await clear.ExecuteNonQueryAsync(cancellationToken);
        }

        var operations = model.operations_csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => v.Trim().ToLowerInvariant())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var operation in operations)
        {
            await using var insertOp = connection.CreateCommand();
            insertOp.CommandText = "INSERT OR IGNORE INTO rbac_role_type_operations (role_type, operation) VALUES ($role_type, $operation);";
            insertOp.Parameters.AddWithValue("$role_type", model.name);
            insertOp.Parameters.AddWithValue("$operation", operation);
            await insertOp.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void Bind(SqliteCommand command, RoleTypeModel model)
    {
        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$name", model.name);
        command.Parameters.AddWithValue("$operations_csv", model.operations_csv);
        command.Parameters.AddWithValue("$rbac_id", model.rbac_id.ToString());
        command.Parameters.AddWithValue("$created_at", model.created_at.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", model.updated_at.ToString("O"));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());
    }

    private static RoleTypeModel ReadModel(SqliteDataReader reader)
        => new(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            Guid.Parse(reader.GetString(3)),
            DateTimeOffset.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
            Guid.Parse(reader.GetString(6)),
            Guid.Parse(reader.GetString(7)));

    private void EnsureDataDirectoryExists()
    {
        var builder = new SqliteConnectionStringBuilder(_connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource))
            return;

        if (string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
            return;

        var directory = Path.GetDirectoryName(dataSource);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);
    }
}
