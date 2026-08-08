using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Identity;

internal sealed class VendorProfileConfiguration : IEntityTypeConfiguration<VendorProfile>
{
    public void Configure(EntityTypeBuilder<VendorProfile> builder)
    {
        builder.ToTable("vendor_profiles", "identity");
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.BusinessName).HasMaxLength(200).IsRequired();
        builder.Property(profile => profile.BusinessRegistrationNo).HasMaxLength(100);
        builder.Property(profile => profile.TaxCode).HasMaxLength(50);
        builder.Property(profile => profile.ApprovalStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.ConfigureAuditableRecord();
        builder.HasOne<User>()
            .WithOne(user => user.VendorProfile)
            .HasForeignKey<VendorProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
