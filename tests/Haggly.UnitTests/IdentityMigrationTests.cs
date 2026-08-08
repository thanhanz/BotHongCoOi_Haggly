using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Haggly.UnitTests;

public sealed class IdentityMigrationTests
{
    [Fact]
    public void Identity_roles_are_seeded_with_all_fixed_role_codes()
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
    public void Initial_identity_migration_is_registered()
    {
        using var context = CreateContext();

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.Contains("InitialIdentity", StringComparison.Ordinal));
    }

    private static HagglyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres")
            .Options;

        return new HagglyDbContext(options);
    }
}
