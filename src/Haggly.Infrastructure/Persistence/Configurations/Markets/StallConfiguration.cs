using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Markets;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Markets;

internal sealed class StallConfiguration : IEntityTypeConfiguration<Stall>
{
    public void Configure(EntityTypeBuilder<Stall> builder)
    {
        builder.ToTable("stalls", "markets");
        builder.HasKey(stall => stall.Id);
        builder.HasIndex(stall => new { stall.MarketId, stall.Code }).IsUnique();
        builder.Property(stall => stall.Code).HasMaxLength(50).IsRequired();
        builder.Property(stall => stall.Name).HasMaxLength(200).IsRequired();
        builder.Property(stall => stall.LocationDescription).HasMaxLength(500);
        builder.Property(stall => stall.PhoneNumber).HasMaxLength(32);
        builder.Property(stall => stall.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.ConfigureSoftDeletable();
        builder.HasQueryFilter(stall => stall.DeletedAt == null);

        builder.HasOne(stall => stall.Market)
            .WithMany(market => market.Stalls)
            .HasForeignKey(stall => stall.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(stall => stall.Vendor)
            .WithMany()
            .HasForeignKey(stall => stall.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
