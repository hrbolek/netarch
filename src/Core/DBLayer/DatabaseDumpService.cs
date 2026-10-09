using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace WebAppArchitecture.DBLayer;

public sealed class DatabaseDumpService(ApplicationDbContext db) : IDatabaseDumpService
{
    private sealed record DumpValue(string kind, string? value);
    private sealed record DumpTable(string name, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<DumpValue>> rows);
    private sealed record DatabaseDump(int format, DateTimeOffset created_at, IReadOnlyList<DumpTable> tables);

    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        var mustClose = connection.State != ConnectionState.Open;
        if (mustClose) await connection.OpenAsync(cancellationToken);

        try
        {
            var tableNames = new List<string>();
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    tableNames.Add(reader.GetString(0));
            }

            var tables = new List<DumpTable>();
            foreach (var tableName in tableNames)
            {
                var columns = new List<string>();
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"")}\")";
                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                        columns.Add(reader.GetString(1));
                }

                var rows = new List<IReadOnlyList<DumpValue>>();
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM \"{tableName.Replace("\"", "\"\"")}\"";
                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var row = new List<DumpValue>(reader.FieldCount);
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            if (await reader.IsDBNullAsync(i, cancellationToken)) { row.Add(new("null", null)); continue; }
                            var value = reader.GetValue(i);
                            row.Add(value switch
                            {
                                byte[] bytes => new("blob", Convert.ToBase64String(bytes)),
                                long l => new("integer", l.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                int n => new("integer", n.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                double d => new("real", d.ToString("R", System.Globalization.CultureInfo.InvariantCulture)),
                                float f => new("real", f.ToString("R", System.Globalization.CultureInfo.InvariantCulture)),
                                decimal m => new("real", m.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                _ => new("text", Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture))
                            });
                        }
                        rows.Add(row);
                    }
                }
                tables.Add(new(tableName, columns, rows));
            }

            var dump = new DatabaseDump(1, DateTimeOffset.UtcNow, tables);
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var temp = path + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(dump, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            File.Move(temp, path, true);
        }
        finally
        {
            if (mustClose) await connection.CloseAsync();
        }
    }

    public async Task<bool> LoadIfExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) return false;

        var dump = JsonSerializer.Deserialize<DatabaseDump>(await File.ReadAllTextAsync(path, cancellationToken))
            ?? throw new InvalidOperationException($"Database dump '{path}' is empty or invalid.");
        if (dump.format != 1) throw new InvalidOperationException($"Unsupported database dump format {dump.format}.");

        await db.Database.EnsureCreatedAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        var mustClose = connection.State != ConnectionState.Open;
        if (mustClose) await connection.OpenAsync(cancellationToken);

        try
        {
            await using (var pragma = connection.CreateCommand()) { pragma.CommandText = "PRAGMA foreign_keys=OFF"; await pragma.ExecuteNonQueryAsync(cancellationToken); }
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var table in dump.tables)
                {
                    await using var clear = connection.CreateCommand();
                    clear.Transaction = tx;
                    clear.CommandText = $"DELETE FROM \"{table.name.Replace("\"", "\"\"")}\"";
                    await clear.ExecuteNonQueryAsync(cancellationToken);
                }

                foreach (var table in dump.tables)
                {
                    if (table.columns.Count == 0) continue;
                    var quotedColumns = string.Join(",", table.columns.Select(c => $"\"{c.Replace("\"", "\"\"")}\""));
                    var parameterNames = string.Join(",", table.columns.Select((_, i) => $"@p{i}"));
                    foreach (var row in table.rows)
                    {
                        await using var insert = connection.CreateCommand();
                        insert.Transaction = tx;
                        insert.CommandText = $"INSERT INTO \"{table.name.Replace("\"", "\"\"")}\" ({quotedColumns}) VALUES ({parameterNames})";
                        for (var i = 0; i < table.columns.Count; i++)
                        {
                            var value = row[i];
                            var parameter = insert.CreateParameter();
                            parameter.ParameterName = $"@p{i}";
                            parameter.Value = value.kind switch
                            {
                                "null" => DBNull.Value,
                                "blob" => Convert.FromBase64String(value.value ?? string.Empty),
                                "integer" => long.Parse(value.value ?? "0", System.Globalization.CultureInfo.InvariantCulture),
                                "real" => double.Parse(value.value ?? "0", System.Globalization.CultureInfo.InvariantCulture),
                                _ => value.value ?? string.Empty
                            };
                            insert.Parameters.Add(parameter);
                        }
                        await insert.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                await tx.CommitAsync(cancellationToken);
            }
            catch { await tx.RollbackAsync(cancellationToken); throw; }
            finally
            {
                await using var pragmaOn = connection.CreateCommand();
                pragmaOn.CommandText = "PRAGMA foreign_keys=ON";
                await pragmaOn.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            if (mustClose) await connection.CloseAsync();
        }
        return true;
    }
}
