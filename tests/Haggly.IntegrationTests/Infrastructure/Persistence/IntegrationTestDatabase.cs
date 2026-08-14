using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Haggly.IntegrationTests.Infrastructure.Persistence;

internal static class IntegrationTestDatabase
{
    private const string DatabaseName = "haggly_test";
    private static readonly Lazy<string> ConnectionStringHolder = new(Initialize);

    public static string ConnectionString => ConnectionStringHolder.Value;

    public static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HagglyDatabase"] = ConnectionString
            })
            .Build();

    private static string Initialize()
    {
        var connectionString = Environment.GetEnvironmentVariable("HAGGLY_TEST_CONNECTION_STRING")
            ?? "Host=localhost;Port=5433;Database=haggly_test;Username=postgres;Password=1234";
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (!string.Equals(builder.Database, DatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Integration tests may only use database '{DatabaseName}', but '{builder.Database}' was configured.");
        }

        CreateDatabaseIfMissing(builder);

        var options = new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;
        using var dbContext = new HagglyDbContext(options);
        dbContext.Database.Migrate();

        return builder.ConnectionString;
    }

    private static void CreateDatabaseIfMissing(NpgsqlConnectionStringBuilder testBuilder)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(testBuilder.ConnectionString)
        {
            Database = "postgres"
        };

        using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        connection.Open();

        using var existsCommand = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @DatabaseName;",
            connection);
        existsCommand.Parameters.AddWithValue("DatabaseName", DatabaseName);

        if (existsCommand.ExecuteScalar() is not null)
            return;

        using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{DatabaseName}\";", connection);
        createCommand.ExecuteNonQuery();
    }
}
