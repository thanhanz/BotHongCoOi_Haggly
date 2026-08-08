using Haggly.Domain.Common;
using Haggly.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence;

internal static class IdentityModelConfiguration
{
    private const string Schema = "identity";

    public static void ApplyIdentityConfiguration(this ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder.Entity<User>());
        ConfigureRole(modelBuilder.Entity<Role>());
        ConfigureUserRole(modelBuilder.Entity<UserRole>());
        ConfigureBuyerProfile(modelBuilder.Entity<BuyerProfile>());
        ConfigureVendorProfile(modelBuilder.Entity<VendorProfile>());
        ConfigureAdminProfile(modelBuilder.Entity<AdminProfile>());
        ConfigureDelivererProfile(modelBuilder.Entity<DelivererProfile>());
    }

    private static void ConfigureUser(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", Schema);
        builder.HasKey(user => user.Id);
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.PhoneNumber).HasMaxLength(32).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(user => user.FullName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.AvatarUrl).HasMaxLength(2048);
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        ConfigureSoftDeletable(builder);
        builder.HasQueryFilter(user => user.DeletedAt == null);

        builder.HasMany(user => user.UserRoles)
            .WithOne(userRole => userRole.User)
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureRole(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", Schema);
        builder.HasKey(role => role.Id);
        builder.HasIndex(role => role.Code).IsUnique();
        builder.Property(role => role.Code).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(role => role.Name).HasMaxLength(100).IsRequired();
        builder.Property(role => role.Description).HasMaxLength(1000);
        ConfigureSoftDeletable(builder);
        builder.HasQueryFilter(role => role.DeletedAt == null);

        builder.HasMany(role => role.UserRoles)
            .WithOne(userRole => userRole.Role)
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUserRole(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", Schema);
        builder.HasKey(userRole => userRole.Id);
        builder.HasIndex(userRole => new { userRole.UserId, userRole.RoleId }).IsUnique();
        builder.Property(userRole => userRole.AssignedAt).IsRequired();
        builder.Property(userRole => userRole.IsActive).IsRequired();
        ConfigureAuditable(builder);
    }

    private static void ConfigureBuyerProfile(EntityTypeBuilder<BuyerProfile> builder)
    {
        builder.ToTable("buyer_profiles", Schema);
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.DefaultPickupNote).HasMaxLength(1000);
        ConfigureRecord(builder);
        builder.HasOne<User>()
            .WithOne(user => user.BuyerProfile)
            .HasForeignKey<BuyerProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureVendorProfile(EntityTypeBuilder<VendorProfile> builder)
    {
        builder.ToTable("vendor_profiles", Schema);
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.BusinessName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.BusinessRegistrationNo).HasMaxLength(100);
        builder.Property(profile => profile.TaxCode).HasMaxLength(50);
        builder.Property(profile => profile.ApprovalStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        ConfigureRecord(builder);
        builder.HasOne<User>()
            .WithOne(user => user.VendorProfile)
            .HasForeignKey<VendorProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAdminProfile(EntityTypeBuilder<AdminProfile> builder)
    {
        builder.ToTable("admin_profiles", Schema);
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.EmployeeCode).HasMaxLength(100);
        builder.Property(profile => profile.AdminScope).HasConversion<string>().HasMaxLength(32).IsRequired();
        ConfigureRecord(builder);
        builder.HasOne<User>()
            .WithOne(user => user.AdminProfile)
            .HasForeignKey<AdminProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDelivererProfile(EntityTypeBuilder<DelivererProfile> builder)
    {
        builder.ToTable("deliverer_profiles", Schema);
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.VehicleType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(profile => profile.VehiclePlate).HasMaxLength(32).IsRequired();
        builder.Property(profile => profile.ApprovalStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        ConfigureRecord(builder);
        builder.HasOne<User>()
            .WithOne(user => user.DelivererProfile)
            .HasForeignKey<DelivererProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAuditable<T>(EntityTypeBuilder<T> builder)
        where T : AuditableEntity
    {
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.CreatedBy);
        builder.Property(entity => entity.UpdatedAt);
        builder.Property(entity => entity.UpdatedBy);
    }

    private static void ConfigureSoftDeletable<T>(EntityTypeBuilder<T> builder)
        where T : SoftDeletableEntity
    {
        ConfigureAuditable(builder);
        builder.Property(entity => entity.DeletedAt);
        builder.Property(entity => entity.DeletedBy);
    }

    private static void ConfigureRecord<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property("CreatedAt").IsRequired();
        builder.Property("CreatedBy");
        builder.Property("UpdatedAt");
        builder.Property("UpdatedBy");
    }
}
