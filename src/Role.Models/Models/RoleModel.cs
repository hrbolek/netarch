using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Role.Models;

public sealed record RoleModel(
    Guid id,
    Guid group_id,
    Guid user_id,
    string role_type,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid? delegated_by_role_id,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record RoleResponse(
    Guid id,
    Guid group_id,
    Guid user_id,
    string role_type,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid? delegated_by_role_id,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDTOModel<RoleModel, RoleResponse>, ISecuredEntityDTOModel
{
    public static RoleResponse FromModel(RoleModel model) => new(
        model.id,
        model.group_id,
        model.user_id,
        model.role_type,
        model.is_deputy,
        model.valid_from,
        model.valid_to,
        model.delegated_by_role_id,
        model.rbac_id,
        model.created_at,
        model.updated_at,
        model.created_by,
        model.updated_by);

    public RoleModel ToModel() => new(
        id,
        group_id,
        user_id,
        role_type,
        is_deputy,
        valid_from,
        valid_to,
        delegated_by_role_id,
        rbac_id,
        created_at,
        updated_at,
        created_by,
        updated_by);
}

public sealed record RoleCreateRequest(
    Guid group_id,
    Guid user_id,
    string role_type,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    Guid? delegated_by_role_id = null,
    Guid? rbac_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<RoleModel>, ISecuredCreateRequest
{
    public RoleModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        return new(
            Guid.NewGuid(),
            group_id,
            user_id,
            role_type,
            is_deputy,
            valid_from,
            valid_to,
            delegated_by_role_id,
            rbac_id ?? RbacWellKnown.RootSecurityObjectId,
            created_at ?? now,
            updated_at ?? now,
            created_by ?? Guid.Empty,
            updated_by ?? Guid.Empty);
    }
}

public sealed record RoleUpdateRequest(
    Guid id,
    string role_type,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<RoleModel>, IEntityDTOModel<RoleModel, RoleUpdateRequest>
{
    public static RoleUpdateRequest FromModel(RoleModel model) => new(
        model.id,
        model.role_type,
        model.is_deputy,
        model.valid_from,
        model.valid_to,
        model.updated_at,
        model.updated_by);

    public RoleModel ApplyTo(RoleModel model)
        => model with
        {
            role_type = role_type,
            is_deputy = is_deputy,
            valid_from = valid_from,
            valid_to = valid_to,
            updated_at = updated_at ?? DateTimeOffset.UtcNow,
            updated_by = updated_by ?? model.updated_by
        };

    public RoleModel ToModel() => new(
        id,
        Guid.Empty,
        Guid.Empty,
        role_type,
        is_deputy,
        valid_from,
        valid_to,
        null,
        RbacWellKnown.RootSecurityObjectId,
        DateTimeOffset.UtcNow,
        updated_at ?? DateTimeOffset.UtcNow,
        Guid.Empty,
        updated_by ?? Guid.Empty);
}

public interface IRoleStore : IBaseStore<RoleModel>
{
}

public sealed class SqliteRoleStore : IRoleStore
{
    private readonly string _connectionString;

    public SqliteRoleStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureDatabase();
    }

    public async Task<RoleModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, group_id, user_id, role_type, is_deputy, valid_from, valid_to, delegated_by_role_id, rbac_id, created_at, updated_at, created_by, updated_by
            FROM rbac_group_roles
            WHERE id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadModel(reader) : null;
    }

    public async Task<IReadOnlyCollection<RoleModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, group_id, user_id, role_type, is_deputy, valid_from, valid_to, delegated_by_role_id, rbac_id, created_at, updated_at, created_by, updated_by
            FROM rbac_group_roles;
            """;

        var items = new List<RoleModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(ReadModel(reader));

        return items;
    }

    public async Task<IReadOnlyCollection<RoleModel>> VectorAsync(Expression<Func<RoleModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(predicate.Compile()).ToArray();
    }

    public async Task<RoleModel> CreateAsync(RoleModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO rbac_group_roles
                (id, group_id, user_id, role_type, is_deputy, valid_from, valid_to, delegated_by_role_id, rbac_id, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $group_id, $user_id, $role_type, $is_deputy, $valid_from, $valid_to, $delegated_by_role_id, $rbac_id, $created_at, $updated_at, $created_by, $updated_by);
            """;
        Bind(command, model);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return model;
    }

    public async Task<RoleModel> UpdateAsync(RoleModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE rbac_group_roles
            SET group_id = $group_id,
                user_id = $user_id,
                role_type = $role_type,
                is_deputy = $is_deputy,
                valid_from = $valid_from,
                valid_to = $valid_to,
                delegated_by_role_id = $delegated_by_role_id,
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
            throw new KeyNotFoundException($"Role '{model.id}' was not found.");

        return model;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM rbac_group_roles WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void EnsureDatabase()
    {
        EnsureDataDirectoryExists();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var create = connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE IF NOT EXISTS rbac_group_roles (
                id TEXT PRIMARY KEY,
                group_id TEXT NOT NULL,
                user_id TEXT NOT NULL,
                role_type TEXT NOT NULL,
                is_deputy INTEGER NOT NULL,
                valid_from TEXT NOT NULL,
                valid_to TEXT NULL,
                delegated_by_role_id TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                created_by TEXT NOT NULL,
                updated_by TEXT NOT NULL
            );
            """;
        create.ExecuteNonQuery();

        EnsureColumnExists(connection, "rbac_group_roles", "rbac_id", $"TEXT NOT NULL DEFAULT '{RbacWellKnown.RootSecurityObjectId}'");
    }

    private static void Bind(SqliteCommand command, RoleModel model)
    {
        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$group_id", model.group_id.ToString());
        command.Parameters.AddWithValue("$user_id", model.user_id.ToString());
        command.Parameters.AddWithValue("$role_type", model.role_type);
        command.Parameters.AddWithValue("$is_deputy", model.is_deputy ? 1 : 0);
        command.Parameters.AddWithValue("$valid_from", model.valid_from.ToString("O"));
        command.Parameters.AddWithValue("$valid_to", model.valid_to is null ? (object)DBNull.Value : model.valid_to.Value.ToString("O"));
        command.Parameters.AddWithValue("$delegated_by_role_id", model.delegated_by_role_id?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$rbac_id", model.rbac_id.ToString());
        command.Parameters.AddWithValue("$created_at", model.created_at.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", model.updated_at.ToString("O"));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());
    }

    private static RoleModel ReadModel(SqliteDataReader reader)
        => new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            reader.GetString(3),
            reader.GetInt64(4) != 0,
            DateTimeOffset.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.IsDBNull(6) ? null : DateTimeOffset.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.IsDBNull(7) ? null : Guid.Parse(reader.GetString(7)),
            Guid.Parse(reader.GetString(8)),
            DateTimeOffset.Parse(reader.GetString(9), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(reader.GetString(10), null, System.Globalization.DateTimeStyles.RoundtripKind),
            Guid.Parse(reader.GetString(11)),
            Guid.Parse(reader.GetString(12)));

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
