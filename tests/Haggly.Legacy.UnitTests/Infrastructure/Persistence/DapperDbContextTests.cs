using Haggly.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence;

public sealed class DapperDbContextTests
{
    [Fact]
    public void CreateConnection_WhenConnectionStringIsConfigured_ReturnsNpgsqlConnection()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:HagglyDatabase"] =
                "Host=localhost;Port=5433;Database=haggly;Username=postgres;Password=1234"
        };
        var context = new DapperDbContext(configuration);

        using var connection = context.CreateConnection();

        Assert.IsType<NpgsqlConnection>(connection);
        Assert.Contains("Database=haggly", connection.ConnectionString);
    }

    [Fact]
    public void CreateConnection_WhenConnectionStringIsMissing_ThrowsConfigurationException()
    {
        var configuration = new ConfigurationManager();

        var exception = Assert.Throws<InvalidOperationException>(
            () => new DapperDbContext(configuration));

        Assert.Contains("ConnectionStrings:HagglyDatabase", exception.Message);
    }
}
