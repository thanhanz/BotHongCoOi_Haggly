using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Identity;

internal sealed class BuyerProfileConfiguration : IEntityTypeConfiguration<BuyerProfile>
{
    public void Configure(EntityTypeBuilder<BuyerProfile> builder)
    {
        builder.ToTable("buyer_profiles", "identity");
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.DefaultPickupNote).HasMaxLength(1000);
        builder.ConfigureAuditableRecord();
        builder.HasOne<User>()
            .WithOne(user => user.BuyerProfile)
            .HasForeignKey<BuyerProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
