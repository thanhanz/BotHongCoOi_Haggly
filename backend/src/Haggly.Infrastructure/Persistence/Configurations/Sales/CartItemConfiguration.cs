using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Sales;

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items", "sales", table =>
        {
            table.HasCheckConstraint(
                "CK_cart_items_quantity_bounds",
                "\"Quantity\" > 0");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.CartId, item.InventoryItemId }).IsUnique();
        builder.Property(item => item.CartId).IsRequired();
        builder.Property(item => item.InventoryItemId).IsRequired();
        builder.Property(item => item.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(item => item.Notes).HasMaxLength(500);
        builder.ConfigureAuditable();

        builder.HasOne(item => item.Cart)
            .WithMany(cart => cart.Items)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<InventoryItem>()
            .WithMany()
            .HasForeignKey(item => item.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
