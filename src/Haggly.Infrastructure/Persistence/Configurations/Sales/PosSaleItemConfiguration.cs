using Haggly.Domain.Modules.Sales;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Sales;

internal sealed class PosSaleItemConfiguration : IEntityTypeConfiguration<PosSaleItem>
{
    public void Configure(EntityTypeBuilder<PosSaleItem> builder)
    {
        builder.ToTable("pos_sale_items", "sales", table =>
        {
            table.HasCheckConstraint("CK_pos_sale_items_quantity_bounds", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_pos_sale_items_price_bounds", "\"UnitPrice\" >= 0 AND \"LineTotal\" >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.PosSaleId, item.DailyProductListingId }).IsUnique();
        builder.Property(item => item.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(item => item.SellingUnitSnapshot)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(item => item.LineTotal).HasPrecision(18, 2).IsRequired();
        builder.ConfigureAuditable();
    }
}
