using Haggly.Domain.Modules.Markets;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Markets;

internal sealed class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.ToTable("markets", "markets");
        builder.HasKey(market => market.Id);
        builder.HasIndex(market => market.Code).IsUnique();
        builder.Property(market => market.Code).HasMaxLength(50).IsRequired();
        builder.Property(market => market.Name).HasMaxLength(200).IsRequired();
        builder.Property(market => market.Address).HasMaxLength(500).IsRequired();
        builder.Property(market => market.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.ConfigureSoftDeletable();
        builder.HasQueryFilter(market => market.DeletedAt == null);

        builder.HasMany(market => market.Stalls)
            .WithOne(stall => stall.Market)
            .HasForeignKey(stall => stall.MarketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
