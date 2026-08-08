using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Identity;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "identity");
        builder.HasKey(role => role.Id);
        builder.HasIndex(role => role.Code).IsUnique();
        builder.Property(role => role.Code).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(role => role.Name).HasMaxLength(100).IsRequired();
        builder.Property(role => role.Description).HasMaxLength(1000);
        builder.ConfigureSoftDeletable();
        builder.HasQueryFilter(role => role.DeletedAt == null);

        builder.HasMany(role => role.UserRoles)
            .WithOne(userRole => userRole.Role)
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new
            {
                Id = RoleSeedIds.Buyer,
                Code = RoleCode.BUYER,
                Name = "Buyer",
                Description = "A market buyer.",
                IsActive = true,
                CreatedAt = RoleSeedIds.CreatedAt
            },
            new
            {
                Id = RoleSeedIds.Vendor,
                Code = RoleCode.VENDOR,
                Name = "Vendor",
                Description = "A market vendor.",
                IsActive = true,
                CreatedAt = RoleSeedIds.CreatedAt
            },
            new
            {
                Id = RoleSeedIds.MarketAdmin,
                Code = RoleCode.MARKET_ADMIN,
                Name = "Market administrator",
                Description = "An administrator for market operations.",
                IsActive = true,
                CreatedAt = RoleSeedIds.CreatedAt
            },
            new
            {
                Id = RoleSeedIds.PlatformAdmin,
                Code = RoleCode.PLATFORM_ADMIN,
                Name = "Platform administrator",
                Description = "A platform administrator.",
                IsActive = true,
                CreatedAt = RoleSeedIds.CreatedAt
            },
            new
            {
                Id = RoleSeedIds.Deliverer,
                Code = RoleCode.DELIVERER,
                Name = "Deliverer",
                Description = "A delivery operator.",
                IsActive = true,
                CreatedAt = RoleSeedIds.CreatedAt
            });
    }

    private static class RoleSeedIds
    {
        public static readonly Guid Buyer = new("10000000-0000-0000-0000-000000000001");
        public static readonly Guid Vendor = new("10000000-0000-0000-0000-000000000002");
        public static readonly Guid MarketAdmin = new("10000000-0000-0000-0000-000000000003");
        public static readonly Guid PlatformAdmin = new("10000000-0000-0000-0000-000000000004");
        public static readonly Guid Deliverer = new("10000000-0000-0000-0000-000000000005");
        public static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
