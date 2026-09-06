using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Migrations;

public sealed class SalesOrderMigrationTests
{
    [Fact]
    public void GetMigrations_WhenOrderMigrationExists_ContainsSalesOrderMigration()
    {
        using var context = new HagglyDbContext(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres").Options);

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.Contains("CreateSalesOrders", StringComparison.Ordinal));
    }
}
