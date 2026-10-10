using Npgsql;
using Microsoft.Extensions.Configuration;

namespace WebAppArchitecture.Tests.Shared;

/// <summary>Owns one disposable PostgreSQL database; never points to development.</summary>
public sealed class PostgresTestDatabase : IDisposable
{
    private readonly string databaseName = "za78_" + Guid.NewGuid().ToString("N");
    private readonly string adminConnectionString;
    public string ConnectionString { get; }

    public PostgresTestDatabase()
    {
        // This points to the test server, NOT the development server.
        adminConnectionString = ReadHostTestConnectionString();
        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString);
        if (builder.Host != "postgres_test")
            throw new InvalidOperationException("Test isolation requires Host=postgres_test; refusing to use another server.");
        builder.Database = databaseName;
        ConnectionString = builder.ConnectionString;
        using var connection = new NpgsqlConnection(adminConnectionString);
        connection.Open();
        using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        command.ExecuteNonQuery();
    }

    private static string ReadHostTestConnectionString()
    {
        // The test connection belongs to Host configuration, not environment variables.
        foreach (var origin in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(origin); directory is not null; directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, "src", "Host", "appsettings.Development.json");
                if (!File.Exists(path)) continue;
                var config = new ConfigurationBuilder().AddJsonFile(path).Build();
                return config.GetConnectionString("IntegrationTests")
                    ?? throw new InvalidOperationException("Host config has no IntegrationTests connection string.");
            }
        }
        throw new FileNotFoundException("Cannot locate src/Host/appsettings.Development.json for PostgreSQL integration tests.");
    }

    public void Dispose()
    {
        NpgsqlConnection.ClearAllPools();
        using var connection = new NpgsqlConnection(adminConnectionString);
        connection.Open();
        using var terminate = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity " +
            "WHERE datname = @db AND pid <> pg_backend_pid()", connection);
        terminate.Parameters.AddWithValue("db", databaseName);
        terminate.ExecuteNonQuery();
        using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", connection);
        command.ExecuteNonQuery();
    }
}
