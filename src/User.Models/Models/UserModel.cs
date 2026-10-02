using WebAppArchitecture.Base.Data;
using System.Linq.Expressions;
using Microsoft.Data.Sqlite;

namespace WebAppArchitecture.User.Models;

/// <summary>Persistence/data model.</summary>
public sealed record UserModel(
    Guid id,
    string Name,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

/// <summary>Public resolver/UI DTO kept outside the resolver assembly.</summary>
public sealed record UserResponse(
    Guid id,
    string Name,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDTOModel<UserModel, UserResponse>, ISecuredEntityDTOModel
{
    public static UserResponse FromModel(UserModel model)
        => new(
            model.id,
            model.Name,
            model.rbac_id,
            model.created_at,
            model.updated_at,
            model.created_by,
            model.updated_by
        );

    public UserModel ToModel()
        => new(
            id,
            Name,
            rbac_id,
            created_at,
            updated_at,
            created_by,
            updated_by
        );        
};

public sealed record UserCreateRequest(
    string Name,
    Guid? rbac_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<UserModel>, ISecuredCreateRequest
{
    public UserModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;

        return new(
            Guid.NewGuid(),
            Name,
            rbac_id ?? WebAppArchitecture.Base.Security.RbacWellKnown.RootSecurityObjectId,
            created_at ?? now,
            updated_at ?? now,
            created_by ?? Guid.Empty,
            updated_by ?? Guid.Empty
        );
    }
};

public sealed record UserUpdateRequest(
    Guid id,
    string Name,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<UserModel>, IEntityDTOModel<UserModel, UserUpdateRequest>
{
    public static UserUpdateRequest FromModel(UserModel model)
        => new(
            model.id,
            model.Name,
            model.updated_at,
            model.updated_by
        );

    public UserModel ApplyTo(UserModel model)
        => model with
        {
            Name = Name,
            updated_at = updated_at ?? DateTimeOffset.UtcNow,
            updated_by = updated_by ?? model.updated_by
        };

    public UserModel ToModel()
        => new(
            id,
            Name,
            WebAppArchitecture.Base.Security.RbacWellKnown.RootSecurityObjectId,
            DateTimeOffset.UtcNow,
            updated_at ?? DateTimeOffset.UtcNow,
            Guid.Empty,
            updated_by ?? Guid.Empty
        );
};
public interface IUserStore : IBaseStore<UserModel>
{
}

/// <summary>Demo store persistent for the lifetime of the host process.</summary>
public sealed class InMemoryUserStore
    : InMemoryStore<UserModel>, IUserStore
{
}

/// <summary>
/// SQLite-backed store persisting user data across process restarts.
/// </summary>
public sealed class SqliteUserStore : IUserStore
{
    private const string TableName = "users";
    private readonly string _connectionString;

    public SqliteUserStore(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));

        _connectionString = connectionString;
        EnsureDatabase();
    }

    public async Task<UserModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT id, name, rbac_id, created_at, updated_at, created_by, updated_by
            FROM {TableName}
            WHERE id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new UserModel(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            Guid.Parse(reader.GetString(2)),
            DateTimeOffset.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
            Guid.Parse(reader.GetString(5)),
            Guid.Parse(reader.GetString(6)));
    }

    public async Task<IReadOnlyCollection<UserModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT id, name, rbac_id, created_at, updated_at, created_by, updated_by
            FROM {TableName};
            """;

        var result = new List<UserModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new UserModel(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                Guid.Parse(reader.GetString(2)),
                DateTimeOffset.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
                DateTimeOffset.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
                Guid.Parse(reader.GetString(5)),
                Guid.Parse(reader.GetString(6))));
        }

        return result;
    }

    public async Task<IReadOnlyCollection<UserModel>> VectorAsync(
        Expression<Func<UserModel, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var compiled = predicate.Compile();
        var models = await GetAllAsync(cancellationToken);

        return models.Where(compiled).ToArray();
    }

    public async Task<UserModel> CreateAsync(UserModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO {TableName}(id, name, rbac_id, created_at, updated_at, created_by, updated_by)
            VALUES ($id, $name, $rbac_id, $created_at, $updated_at, $created_by, $updated_by);
            """;
        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$name", model.Name);
        command.Parameters.AddWithValue("$rbac_id", model.rbac_id.ToString());
        command.Parameters.AddWithValue("$created_at", model.created_at.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", model.updated_at.ToString("O"));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return model;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException($"Item with id '{model.id}' already exists.", ex);
        }
    }

    public async Task<UserModel> UpdateAsync(UserModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            UPDATE {TableName}
            SET name = $name,
                rbac_id = $rbac_id,
                created_at = $created_at,
                updated_at = $updated_at,
                created_by = $created_by,
                updated_by = $updated_by
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$name", model.Name);
        command.Parameters.AddWithValue("$rbac_id", model.rbac_id.ToString());
        command.Parameters.AddWithValue("$created_at", model.created_at.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", model.updated_at.ToString("O"));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
            throw new KeyNotFoundException($"Item with id '{model.id}' was not found.");

        return model;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            DELETE FROM {TableName}
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection CreateConnection()
        => new(_connectionString);

    private void EnsureDatabase()
    {
        EnsureDataDirectoryExists();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {TableName} (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                rbac_id TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                created_by TEXT NOT NULL,
                updated_by TEXT NOT NULL
            );
            """;

        command.ExecuteNonQuery();

        EnsureColumnExists(connection, TableName, "rbac_id", "TEXT NOT NULL DEFAULT '20000000-0000-0000-0000-000000000001'");
        EnsureColumnExists(connection, TableName, "created_at", "TEXT NOT NULL DEFAULT '1970-01-01T00:00:00.0000000+00:00'");
        EnsureColumnExists(connection, TableName, "updated_at", "TEXT NOT NULL DEFAULT '1970-01-01T00:00:00.0000000+00:00'");
        EnsureColumnExists(connection, TableName, "created_by", "TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
        EnsureColumnExists(connection, TableName, "updated_by", "TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'");
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

    private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        using var pragma = connection.CreateCommand();
        pragma.CommandText = $"PRAGMA table_info({tableName});";

        var hasColumn = false;
        using (var reader = pragma.ExecuteReader())
        {
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    hasColumn = true;
                    break;
                }
            }
        }

        if (hasColumn)
            return;

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alter.ExecuteNonQuery();
    }
}
