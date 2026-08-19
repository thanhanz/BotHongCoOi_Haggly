using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Sales;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", "sales", table =>
        {
            table.HasCheckConstraint(
                "CK_order_items_amount_bounds",
                "\"PublicUnitPriceSnapshot\" >= 0 AND \"FinalUnitPrice\" >= 0 AND \"FinalQuantity\" > 0 AND \"LineTotal\" >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.StallFulfillmentId, item.InventoryItemId }).IsUnique();
        builder.Property(item => item.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(item => item.SellingUnitSnapshot).HasMaxLength(32).IsRequired();
        builder.Property(item => item.PublicUnitPriceSnapshot).HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.FinalUnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.FinalQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(item => item.LineTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(item => item.Notes).HasMaxLength(500);
        builder.ConfigureAuditable();
        builder.Ignore(item => item.InventoryReservations);

        builder.HasOne(item => item.InventoryItem)
            .WithMany()
            .HasForeignKey(item => item.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
