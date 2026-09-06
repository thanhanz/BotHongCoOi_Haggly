using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Migrations;

public sealed class OutboxMigrationTests
{
    [Fact]
    public void GetMigrations_WhenOutboxMigrationExists_ContainsCreateOutboxMessagesMigration()
    {
        using var context = new HagglyDbContext(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres")
            .Options);

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.Contains("CreateOutboxMessages", StringComparison.Ordinal));
    }
}
