using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Migrations;

public sealed class IdentityMigrationTests
{
    [Fact]
    public void IdentityRoleSeed_WhenModelIsBuilt_ContainsAllFixedRoleCodes()
    {
        using var context = CreateContext();
        var role = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Role))!;
        var seedCodes = role.GetSeedData()
            .Select(seed => (RoleCode)seed[nameof(Role.Code)]!)
            .ToHashSet();

        Assert.Equal(
            Enum.GetValues<RoleCode>().ToHashSet(),
            seedCodes);
    }

    [Fact]
    public void GetMigrations_WhenIdentityMigrationExists_ContainsInitialIdentity()
    {
        using var context = CreateContext();

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.Contains("InitialIdentity", StringComparison.Ordinal));
    }

    [Fact]
    public void GetMigrations_WhenCategoryMigrationExists_ContainsCreateCategories()
    {
        using var context = CreateContext();

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.Contains("CreateCategories", StringComparison.Ordinal));
    }

    [Fact]
    public void GetMigrations_WhenProductMigrationExists_ContainsCreateProducts()
    {
        using var context = CreateContext();

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.Contains("CreateProducts", StringComparison.Ordinal));
    }

    private static HagglyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres")
            .Options;

        return new HagglyDbContext(options);
    }
}
