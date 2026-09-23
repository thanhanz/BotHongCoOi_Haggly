using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Identity;

internal sealed class DelivererProfileConfiguration : IEntityTypeConfiguration<DelivererProfile>
{
    public void Configure(EntityTypeBuilder<DelivererProfile> builder)
    {
        builder.ToTable("deliverer_profiles", "identity");
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.VehicleType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(profile => profile.VehiclePlate).HasMaxLength(32).IsRequired();
        builder.Property(profile => profile.ApprovalStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.ConfigureAuditableRecord();
        builder.HasOne<User>()
            .WithOne(user => user.DelivererProfile)
            .HasForeignKey<DelivererProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
