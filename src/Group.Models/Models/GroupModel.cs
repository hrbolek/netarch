using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Group.Models;

public sealed record GroupModel(
    Guid id,
    string Name,
    Guid? master_group_id,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record GroupResponse(
    Guid id,
    string Name,
    Guid? master_group_id,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDTOModel<GroupModel, GroupResponse>, ISecuredEntityDTOModel
{
    public static GroupResponse FromModel(GroupModel model) => new(
        model.id,
        model.Name,
        model.master_group_id,
        model.rbac_id,
        model.created_at,
        model.updated_at,
        model.created_by,
        model.updated_by);

    public GroupModel ToModel() => new(
        id,
        Name,
        master_group_id,
        rbac_id,
        created_at,
        updated_at,
        created_by,
        updated_by);
}

public sealed record GroupCreateRequest(
    string Name,
    Guid? master_group_id = null,
    Guid? rbac_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<GroupModel>, ISecuredCreateRequest
{
    public GroupModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        return new(
            Guid.NewGuid(),
            Name,
            master_group_id,
            rbac_id ?? RbacWellKnown.RootSecurityObjectId,
            created_at ?? now,
            updated_at ?? now,
            created_by ?? Guid.Empty,
            updated_by ?? Guid.Empty);
    }
}

public sealed record GroupUpdateRequest(
    Guid id,
    string Name,
    Guid? master_group_id = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<GroupModel>, IEntityDTOModel<GroupModel, GroupUpdateRequest>
{
    public static GroupUpdateRequest FromModel(GroupModel model) => new(
        model.id,
        model.Name,
        model.master_group_id,
        model.updated_at,
        model.updated_by);

    public GroupModel ApplyTo(GroupModel model)
        => model with
        {
            Name = Name,
            master_group_id = master_group_id,
            updated_at = updated_at ?? DateTimeOffset.UtcNow,
            updated_by = updated_by ?? model.updated_by
        };

    public GroupModel ToModel() => new(
        id,
        Name,
        master_group_id,
        RbacWellKnown.RootSecurityObjectId,
        DateTimeOffset.UtcNow,
        updated_at ?? DateTimeOffset.UtcNow,
        Guid.Empty,
        updated_by ?? Guid.Empty);
}

public interface IGroupStore : IBaseStore<GroupModel>
{
}

public sealed class SqliteGroupStore : IGroupStore
{
    private readonly string _connectionString;

    public SqliteGroupStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureDatabase();
    }

    public async Task<GroupModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, master_group_id, rbac_id, created_at, updated_at, created_by, updated_by
            FROM rbac_groups
            WHERE id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadModel(reader)
            : null;
    }

    public async Task<IReadOnlyCollection<GroupModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, master_group_id, rbac_id, created_at, updated_at, created_by, updated_by
            FROM rbac_groups;
            """;

        var result = new List<GroupModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(ReadModel(reader));

        return result;
    }

    public async Task<IReadOnlyCollection<GroupModel>> VectorAsync(Expression<Func<GroupModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(predicate.Compile()).ToArray();
    }

    public async Task<GroupModel> CreateAsync(GroupModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO rbac_groups
                (id, name, master_group_id, rbac_id, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $name, $master_group_id, $rbac_id, $created_at, $updated_at, $created_by, $updated_by);
            """;
        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$name", model.Name);
        command.Parameters.AddWithValue("$master_group_id", model.master_group_id?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$rbac_id", model.rbac_id.ToString());
        command.Parameters.AddWithValue("$created_at", model.created_at.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", model.updated_at.ToString("O"));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
        return model;
    }

    public async Task<GroupModel> UpdateAsync(GroupModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE rbac_groups
            SET name = $name,
                master_group_id = $master_group_id,
                rbac_id = $rbac_id,
                created_at = $created_at,
                updated_at = $updated_at,
                created_by = $created_by,
                updated_by = $updated_by
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$name", model.Name);
        command.Parameters.AddWithValue("$master_group_id", model.master_group_id?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$rbac_id", model.rbac_id.ToString());
        command.Parameters.AddWithValue("$created_at", model.created_at.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", model.updated_at.ToString("O"));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
            throw new KeyNotFoundException($"Group '{model.id}' was not found.");

        return model;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM rbac_groups WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    private void EnsureDatabase()
    {
        EnsureDataDirectoryExists();

        using var connection = CreateConnection();
        connection.Open();

        using var create = connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE IF NOT EXISTS rbac_groups (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                master_group_id TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                created_by TEXT NOT NULL,
                updated_by TEXT NOT NULL
            );
            """;
        create.ExecuteNonQuery();

        EnsureColumnExists(connection, "rbac_groups", "rbac_id", $"TEXT NOT NULL DEFAULT '{RbacWellKnown.RootSecurityObjectId}'");

        using var seed = connection.CreateCommand();
        seed.CommandText = """
            INSERT OR IGNORE INTO rbac_groups
                (id, name, master_group_id, rbac_id, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $name, NULL, $rbac_id, $created_at, $updated_at, $created_by, $updated_by);
            """;
        var now = DateTimeOffset.UtcNow.ToString("O");
        seed.Parameters.AddWithValue("$id", RbacWellKnown.RootGroupId.ToString());
        seed.Parameters.AddWithValue("$name", "ROOT");
        seed.Parameters.AddWithValue("$rbac_id", RbacWellKnown.RootSecurityObjectId.ToString());
        seed.Parameters.AddWithValue("$created_at", now);
        seed.Parameters.AddWithValue("$updated_at", now);
        seed.Parameters.AddWithValue("$created_by", RbacWellKnown.BootstrapAdminUserId.ToString());
        seed.Parameters.AddWithValue("$updated_by", RbacWellKnown.BootstrapAdminUserId.ToString());
        seed.ExecuteNonQuery();
    }

    private static GroupModel ReadModel(SqliteDataReader reader)
        => new(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : Guid.Parse(reader.GetString(2)),
            Guid.Parse(reader.GetString(3)),
            DateTimeOffset.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
            Guid.Parse(reader.GetString(6)),
            Guid.Parse(reader.GetString(7)));

    private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string definition)
    {
        using var pragma = connection.CreateCommand();
        pragma.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = pragma.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return;
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        alter.ExecuteNonQuery();
    }

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
