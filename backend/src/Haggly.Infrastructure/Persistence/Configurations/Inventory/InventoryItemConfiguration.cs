using Haggly.Domain.Modules.Inventory;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items", "inventory", table => table.HasCheckConstraint(
            "CK_inventory_items_quantity_bounds",
            "\"CurrentQuantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"CurrentQuantity\""));
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.InventoryId, item.ProductStallId }).IsUnique();
        builder.Property(item => item.CurrentQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(item => item.ReservedQuantity).HasPrecision(18, 3).IsRequired();
        builder.Ignore(item => item.AvailableQuantity);
        builder.Property(item => item.Version).IsConcurrencyToken().ValueGeneratedNever().IsRequired();
        builder.ConfigureAuditable();
        builder.HasOne(item => item.Inventory).WithMany(inventory => inventory.Items)
            .HasForeignKey(item => item.InventoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ProductStall).WithOne()
            .HasForeignKey<InventoryItem>(item => item.ProductStallId).OnDelete(DeleteBehavior.Restrict);
    }
}
