using Haggly.Domain.Modules.Identity;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Identity;

internal sealed class AdminProfileConfiguration : IEntityTypeConfiguration<AdminProfile>
{
    public void Configure(EntityTypeBuilder<AdminProfile> builder)
    {
        builder.ToTable("admin_profiles", "identity");
        builder.HasKey(profile => profile.UserId);
        builder.Property(profile => profile.EmployeeCode).HasMaxLength(100);
        builder.Property(profile => profile.AdminScope).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.ConfigureAuditableRecord();
        builder.HasOne<User>()
            .WithOne(user => user.AdminProfile)
            .HasForeignKey<AdminProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
