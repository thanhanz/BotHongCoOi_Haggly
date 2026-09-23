using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Haggly.Infrastructure.Persistence;

public sealed class DapperDbContext
{
    private const string ConnectionStringName = "HagglyDatabase";
    private readonly string connectionString;

    public DapperDbContext(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConfigurationPath.Combine("ConnectionStrings", ConnectionStringName)}' is required.");
    }

    public NpgsqlConnection CreateConnection()
        => new(connectionString);

    public async Task<NpgsqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken)
    {
        var connection = CreateConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
