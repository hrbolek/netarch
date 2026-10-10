using System.Globalization;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

// One-off importer for DatabaseDumpService JSON format 1, originally produced from SQLite.
// Designed for the disposable LOCAL DEVELOPMENT PostgreSQL database only.
// Never run against test or production. The Host must be stopped during import.
if (args.Length != 2 || args[1] != "--replace-development")
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/SqliteDumpToPostgres -- <dump.json> --replace-development");
    Console.Error.WriteLine("WARNING: this REPLACES all application data in postgres_development/data.");
    return 2;
}

var dumpPath = Path.GetFullPath(args[0]);
if (!File.Exists(dumpPath)) throw new FileNotFoundException("SQLite JSON dump not found", dumpPath);

// Deliberately read from Host's Development settings, not environment variables.
var configurationPath = Path.GetFullPath("src/Host/appsettings.Development.json");
using var configDoc = JsonDocument.Parse(await File.ReadAllTextAsync(configurationPath));
var connectionString = configDoc.RootElement.GetProperty("ConnectionStrings")
    .GetProperty("DefaultConnection").GetString()
    ?? throw new InvalidOperationException("DefaultConnection is missing.");
var csb = new NpgsqlConnectionStringBuilder(connectionString);
if (!string.Equals(csb.Host, "postgres_development", StringComparison.OrdinalIgnoreCase) ||
    !string.Equals(csb.Database, "data", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Safety check: importer only permits postgres_development / data.");

using var json = JsonDocument.Parse(await File.ReadAllTextAsync(dumpPath));
var root = json.RootElement;
if (root.GetProperty("format").GetInt32() != 1)
    throw new InvalidOperationException("Only SQLite JSON dump format 1 is supported.");
var tables = root.GetProperty("tables").EnumerateArray()
    .Select(t => new DumpTable(
        t.GetProperty("name").GetString() ?? "",
        t.GetProperty("columns").EnumerateArray().Select(c => c.GetString() ?? "").ToArray(),
        t.GetProperty("rows").EnumerateArray().Select(r => r.EnumerateArray()
            .Select(v => new DumpValue(v.GetProperty("kind").GetString() ?? "", v.GetProperty("value").ValueKind == JsonValueKind.Null ? null : v.GetProperty("value").GetString()))
            .ToArray()).ToArray()))
    .ToArray();

if (tables.Length == 0 || tables.Any(t => t.Name.Length == 0 || t.Columns.Length == 0 ||
        t.Rows.Any(r => r.Length != t.Columns.Length)))
    throw new InvalidOperationException("Invalid or empty SQLite dump.");
if (tables.Select(t => t.Name).Distinct(StringComparer.Ordinal).Count() != tables.Length)
    throw new InvalidOperationException("Duplicate table names in dump.");

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

// Host must already have initialized the PostgreSQL database schema using EnsureCreated().
var targetTables = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
await using (var cmd = new NpgsqlCommand("SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public'", connection))
await using (var reader = await cmd.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
    {
        var name = reader.GetString(0);
        if (!targetTables.TryGetValue(name, out var set)) targetTables[name] = set = new(StringComparer.Ordinal);
        set.Add(reader.GetString(1));
    }
}
foreach (var table in tables)
{
    if (!targetTables.TryGetValue(table.Name, out var available))
        throw new InvalidOperationException($"Missing PostgreSQL table: {table.Name}");
    if (table.Columns.Distinct(StringComparer.Ordinal).Count() != table.Columns.Length ||
        table.Columns.Any(column => !available.Contains(column)))
        throw new InvalidOperationException($"PostgreSQL schema mismatch in {table.Name}");
}

// Fail before modifying anything if the dump is from an incompatible model version.
var appTables = targetTables.Keys.Where(x => !x.StartsWith("__", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
var dumpTables = tables.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
if (!appTables.SetEquals(dumpTables))
    throw new InvalidOperationException($"Table sets differ. PostgreSQL-only: {string.Join(", ", appTables.Except(dumpTables))}; dump-only: {string.Join(", ", dumpTables.Except(appTables))}");

Console.WriteLine($"Importing {tables.Length} tables, {tables.Sum(t => t.Rows.Length)} rows to {csb.Host}/{csb.Database}.");
Console.WriteLine("THIS REPLACES EXISTING DATA. Stop the Host and back up the development database first.");

await using var tx = await connection.BeginTransactionAsync();
try
{
    // Must be PostgreSQL superuser. This prevents FK-ordering problems during restore;
    // we explicitly check all FK references before COMMIT below.
    await using (var cmd = new NpgsqlCommand("SET LOCAL session_replication_role = 'replica'", connection, tx))
        await cmd.ExecuteNonQueryAsync();

    // All database tables have been preflighted against the dump. No partial replace.
    var truncateSql = "TRUNCATE TABLE " + string.Join(", ", tables.Select(t => Quote(t.Name))) + " CASCADE";
    await using (var cmd = new NpgsqlCommand(truncateSql, connection, tx))
        await cmd.ExecuteNonQueryAsync();

    foreach (var table in tables)
    {
        var sql = $"INSERT INTO {Quote(table.Name)} ({string.Join(", ", table.Columns.Select(Quote))}) VALUES " +
                  $"({string.Join(", ", Enumerable.Range(0, table.Columns.Length).Select(i => $"@p{i}"))})";
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        for (var i = 0; i < table.Columns.Length; i++)
            cmd.Parameters.Add(new NpgsqlParameter($"p{i}", NpgsqlDbType.Unknown));
        foreach (var row in table.Rows)
        {
            for (var i = 0; i < row.Length; i++)
            {
                var item = row[i];
                var parameter = cmd.Parameters[i];
                if (item.Kind == "blob")
                {
                    parameter.NpgsqlDbType = NpgsqlDbType.Bytea;
                    parameter.Value = Convert.FromBase64String(item.Value ?? "");
                }
                else
                {
                    parameter.NpgsqlDbType = NpgsqlDbType.Unknown;
                    parameter.Value = item.Kind == "null" ? DBNull.Value : item.Value ?? "";
                }
            }
            await cmd.ExecuteNonQueryAsync();
        }
        Console.WriteLine($"  {table.Name}: {table.Rows.Length} rows");
    }

    // Explicitly check FK integrity because session_replication_role suppresses FK triggers.
    await ValidateForeignKeys(connection, tx);
    await tx.CommitAsync();
    Console.WriteLine("COMMITTED. Restart the Host; it will maintain EffectiveAccess projection.");
}
catch
{
    await tx.RollbackAsync();
    Console.Error.WriteLine("Import rolled back. Existing development data has not changed.");
    throw;
}
return 0;

static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

static async Task ValidateForeignKeys(NpgsqlConnection connection, NpgsqlTransaction tx)
{
    const string query = """
        SELECT con.conname, child.relname, parent.relname,
               array_agg(ca.attname ORDER BY k.ordinality),
               array_agg(pa.attname ORDER BY k.ordinality)
        FROM pg_constraint con
        JOIN pg_class child ON child.oid = con.conrelid
        JOIN pg_namespace ns ON ns.oid = child.relnamespace AND ns.nspname = 'public'
        JOIN pg_class parent ON parent.oid = con.confrelid
        CROSS JOIN LATERAL unnest(con.conkey, con.confkey) WITH ORDINALITY AS k(c_attnum, p_attnum, ordinality)
        JOIN pg_attribute ca ON ca.attrelid = con.conrelid AND ca.attnum = k.c_attnum
        JOIN pg_attribute pa ON pa.attrelid = con.confrelid AND pa.attnum = k.p_attnum
        WHERE con.contype = 'f'
        GROUP BY con.oid, con.conname, child.relname, parent.relname
        """;
    var constraints = new List<(string Name, string Child, string Parent, string[] ChildCols, string[] ParentCols)>();
    await using (var cmd = new NpgsqlCommand(query, connection, tx))
    await using (var reader = await cmd.ExecuteReaderAsync())
        while (await reader.ReadAsync())
            constraints.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetFieldValue<string[]>(3), reader.GetFieldValue<string[]>(4)));

    foreach (var fk in constraints)
    {
        var nonNull = string.Join(" AND ", fk.ChildCols.Select(c => $"c.{Quote(c)} IS NOT NULL"));
        var join = string.Join(" AND ", fk.ChildCols.Zip(fk.ParentCols,
            (c, p) => $"c.{Quote(c)} = p.{Quote(p)}"));
        var sql = $"SELECT EXISTS (SELECT 1 FROM {Quote(fk.Child)} c WHERE {nonNull} " +
                  $"AND NOT EXISTS (SELECT 1 FROM {Quote(fk.Parent)} p WHERE {join}))";
        await using var cmd = new NpgsqlCommand(sql, connection, tx);
        if ((bool)(await cmd.ExecuteScalarAsync() ?? false))
            throw new InvalidOperationException($"Foreign key violation after import: {fk.Name} ({fk.Child} -> {fk.Parent})");
    }
}

internal sealed record DumpValue(string Kind, string? Value);
internal sealed record DumpTable(string Name, string[] Columns, DumpValue[][] Rows);
