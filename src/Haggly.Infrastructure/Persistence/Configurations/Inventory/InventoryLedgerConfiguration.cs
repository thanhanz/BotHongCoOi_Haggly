using Haggly.Domain.Modules.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventoryLedgerConfiguration : IEntityTypeConfiguration<InventoryLedger>
{
    public void Configure(EntityTypeBuilder<InventoryLedger> builder)
    {
        builder.ToTable("inventory_ledgers", "inventory", table => table.HasCheckConstraint(
            "CK_inventory_ledgers_quantity_bounds", "\"QuantityBefore\" >= 0 AND \"QuantityAfter\" >= 0"));
        builder.HasKey(ledger => ledger.Id);
        builder.HasIndex(ledger => new { ledger.InventoryId, ledger.OccurredAt, ledger.Id });
        builder.HasIndex(ledger => new { ledger.InventoryItemId, ledger.OccurredAt, ledger.Id });
        builder.Property(ledger => ledger.TransactionType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(ledger => ledger.QuantityDelta).HasPrecision(18, 3).IsRequired();
        builder.Property(ledger => ledger.QuantityBefore).HasPrecision(18, 3).IsRequired();
        builder.Property(ledger => ledger.QuantityAfter).HasPrecision(18, 3).IsRequired();
        builder.Property(ledger => ledger.ReferenceType).HasMaxLength(64).IsRequired();
        builder.Property(ledger => ledger.Reason).HasMaxLength(500);
        builder.Property(ledger => ledger.OccurredAt).IsRequired();
        builder.Property(ledger => ledger.CreatedAt).IsRequired();
        builder.HasOne(ledger => ledger.InventoryItem).WithMany(item => item.InventoryLedgers)
            .HasForeignKey(ledger => ledger.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(ledger => ledger.Inventory).WithMany(inventory => inventory.InventoryLedgers)
            .HasForeignKey(ledger => ledger.InventoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
