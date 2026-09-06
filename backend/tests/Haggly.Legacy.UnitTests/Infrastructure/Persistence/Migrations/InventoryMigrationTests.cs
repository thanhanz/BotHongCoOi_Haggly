using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Migrations;

public sealed class InventoryMigrationTests
{
    [Fact]
    public void GetMigrations_WhenContinuousInventoryMigrationExists_ContainsRefactorMigration()
    {
        using var context = CreateContext();

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.Contains("RefactorContinuousInventory", StringComparison.Ordinal));
    }

    private static HagglyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres")
            .Options;

        return new HagglyDbContext(options);
    }
}
