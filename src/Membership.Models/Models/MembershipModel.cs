using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.Membership.Models;

public sealed record MembershipModel(
    Guid id,
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : ISecuredEntityDBModel;

public sealed record MembershipResponse(
    Guid id,
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid rbac_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
) : IEntityDTOModel<MembershipModel, MembershipResponse>, ISecuredEntityDTOModel
{
    public static MembershipResponse FromModel(MembershipModel model) => new(
        model.id,
        model.group_id,
        model.user_id,
        model.valid_from,
        model.valid_to,
        model.rbac_id,
        model.created_at,
        model.updated_at,
        model.created_by,
        model.updated_by);

    public MembershipModel ToModel() => new(
        id,
        group_id,
        user_id,
        valid_from,
        valid_to,
        rbac_id,
        created_at,
        updated_at,
        created_by,
        updated_by);
}

public sealed record MembershipCreateRequest(
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    Guid? rbac_id = null,
    DateTimeOffset? created_at = null,
    DateTimeOffset? updated_at = null,
    Guid? created_by = null,
    Guid? updated_by = null
) : ICreateDTOModel<MembershipModel>, ISecuredCreateRequest
{
    public MembershipModel ToModel()
    {
        var now = DateTimeOffset.UtcNow;
        return new(
            Guid.NewGuid(),
            group_id,
            user_id,
            valid_from,
            valid_to,
            rbac_id ?? RbacWellKnown.RootSecurityObjectId,
            created_at ?? now,
            updated_at ?? now,
            created_by ?? Guid.Empty,
            updated_by ?? Guid.Empty);
    }
}

public sealed record MembershipUpdateRequest(
    Guid id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    DateTimeOffset? updated_at = null,
    Guid? updated_by = null
) : IUpdateDTOModel<MembershipModel>, IEntityDTOModel<MembershipModel, MembershipUpdateRequest>
{
    public static MembershipUpdateRequest FromModel(MembershipModel model) => new(
        model.id,
        model.valid_from,
        model.valid_to,
        model.updated_at,
        model.updated_by);

    public MembershipModel ApplyTo(MembershipModel model)
        => model with
        {
            valid_from = valid_from,
            valid_to = valid_to,
            updated_at = updated_at ?? DateTimeOffset.UtcNow,
            updated_by = updated_by ?? model.updated_by
        };

    public MembershipModel ToModel() => new(
        id,
        Guid.Empty,
        Guid.Empty,
        valid_from,
        valid_to,
        RbacWellKnown.RootSecurityObjectId,
        DateTimeOffset.UtcNow,
        updated_at ?? DateTimeOffset.UtcNow,
        Guid.Empty,
        updated_by ?? Guid.Empty);
}

public interface IMembershipStore : IBaseStore<MembershipModel>
{
}

public sealed class SqliteMembershipStore : IMembershipStore
{
    private readonly string _connectionString;

    public SqliteMembershipStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureDatabase();
    }

    public async Task<MembershipModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, group_id, user_id, valid_from, valid_to, rbac_id, created_at, updated_at, created_by, updated_by
            FROM rbac_group_memberships
            WHERE id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadModel(reader) : null;
    }

    public async Task<IReadOnlyCollection<MembershipModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, group_id, user_id, valid_from, valid_to, rbac_id, created_at, updated_at, created_by, updated_by
            FROM rbac_group_memberships;
            """;

        var items = new List<MembershipModel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(ReadModel(reader));

        return items;
    }

    public async Task<IReadOnlyCollection<MembershipModel>> VectorAsync(Expression<Func<MembershipModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(predicate.Compile()).ToArray();
    }

    public async Task<MembershipModel> CreateAsync(MembershipModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO rbac_group_memberships
                (id, group_id, user_id, valid_from, valid_to, rbac_id, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $group_id, $user_id, $valid_from, $valid_to, $rbac_id, $created_at, $updated_at, $created_by, $updated_by);
            """;
        Bind(command, model);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return model;
    }

    public async Task<MembershipModel> UpdateAsync(MembershipModel model, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE rbac_group_memberships
            SET group_id = $group_id,
                user_id = $user_id,
                valid_from = $valid_from,
                valid_to = $valid_to,
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
            throw new KeyNotFoundException($"Membership '{model.id}' was not found.");

        return model;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM rbac_group_memberships WHERE id = $id;";
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
            CREATE TABLE IF NOT EXISTS rbac_group_memberships (
                id TEXT PRIMARY KEY,
                group_id TEXT NOT NULL,
                user_id TEXT NOT NULL,
                valid_from TEXT NOT NULL,
                valid_to TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                created_by TEXT NOT NULL,
                updated_by TEXT NOT NULL
            );
            """;
        create.ExecuteNonQuery();

        EnsureColumnExists(connection, "rbac_group_memberships", "rbac_id", $"TEXT NOT NULL DEFAULT '{RbacWellKnown.RootSecurityObjectId}'");
    }

    private static void Bind(SqliteCommand command, MembershipModel model)
    {
        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$group_id", model.group_id.ToString());
        command.Parameters.AddWithValue("$user_id", model.user_id.ToString());
        command.Parameters.AddWithValue("$valid_from", model.valid_from.ToString("O"));
        command.Parameters.AddWithValue("$valid_to", model.valid_to is null ? (object)DBNull.Value : model.valid_to.Value.ToString("O"));
        command.Parameters.AddWithValue("$rbac_id", model.rbac_id.ToString());
        command.Parameters.AddWithValue("$created_at", model.created_at.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", model.updated_at.ToString("O"));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());
    }

    private static MembershipModel ReadModel(SqliteDataReader reader)
        => new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            DateTimeOffset.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
            reader.IsDBNull(4) ? null : DateTimeOffset.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
            Guid.Parse(reader.GetString(5)),
            DateTimeOffset.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind),
            DateTimeOffset.Parse(reader.GetString(7), null, System.Globalization.DateTimeStyles.RoundtripKind),
            Guid.Parse(reader.GetString(8)),
            Guid.Parse(reader.GetString(9)));

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
