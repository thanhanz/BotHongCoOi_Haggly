using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Persistence.Configurations.Identity;

public sealed class IdentityPersistenceModelTests
{
    [Fact]
    public void IdentityModel_WhenModelIsBuilt_MapsEntitiesToIdentitySchema()
    {
        using var context = CreateContext();

        AssertTable(context, typeof(User), "users");
        AssertTable(context, typeof(Role), "roles");
        AssertTable(context, typeof(UserRole), "user_roles");
        AssertTable(context, typeof(BuyerProfile), "buyer_profiles");
        AssertTable(context, typeof(VendorProfile), "vendor_profiles");
        AssertTable(context, typeof(AdminProfile), "admin_profiles");
        AssertTable(context, typeof(DelivererProfile), "deliverer_profiles");
    }

    [Fact]
    public void IdentityModel_WhenRelationshipsAreConfigured_UsesSharedProfileKeysAndRestrictDeletes()
    {
        using var context = CreateContext();

        var user = context.Model.FindEntityType(typeof(User))!;
        var userRole = context.Model.FindEntityType(typeof(UserRole))!;
        var profile = context.Model.FindEntityType(typeof(VendorProfile))!;

        Assert.Equal(nameof(User.Id), user.FindPrimaryKey()!.Properties.Single().Name);
        Assert.Equal(nameof(UserRole.Id), userRole.FindPrimaryKey()!.Properties.Single().Name);
        Assert.Equal(nameof(VendorProfile.UserId), profile.FindPrimaryKey()!.Properties.Single().Name);

        var userForeignKey = userRole.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(User));
        Assert.Equal(DeleteBehavior.Restrict, userForeignKey.DeleteBehavior);

        var vendorForeignKey = profile.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(User));
        Assert.Equal(DeleteBehavior.Cascade, vendorForeignKey.DeleteBehavior);
    }

    [Fact]
    public void IdentityModel_WhenConstraintsAreConfigured_IncludesEmailRoleCodeAndUserRolePair()
    {
        using var context = CreateContext();

        var user = context.Model.FindEntityType(typeof(User))!;
        var role = context.Model.FindEntityType(typeof(Role))!;
        var userRole = context.Model.FindEntityType(typeof(UserRole))!;

        Assert.Contains(user.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(User.Email) }));
        Assert.Contains(role.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Role.Code) }));
        Assert.Contains(userRole.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(UserRole.UserId), nameof(UserRole.RoleId) }));
    }

    [Fact]
    public void IdentityModel_WhenUsersAndRolesAreConfigured_UsesSoftDeleteFilters()
    {
        using var context = CreateContext();

        Assert.NotEmpty(context.Model.FindEntityType(typeof(User))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(Role))!.GetDeclaredQueryFilters());
    }

    private static HagglyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql("Host=localhost;Database=haggly;Username=postgres;Password=postgres")
            .Options;

        return new HagglyDbContext(options);
    }

    private static void AssertTable(DbContext context, Type entityType, string tableName)
    {
        var metadata = context.Model.FindEntityType(entityType)!;

        Assert.Equal(tableName, metadata.GetTableName());
        Assert.Equal("identity", metadata.GetSchema());
    }
}
