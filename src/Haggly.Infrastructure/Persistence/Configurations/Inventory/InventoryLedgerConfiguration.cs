using Haggly.Domain.Modules.Inventory;
using Haggly.Infrastructure.Persistence.Configurations.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Inventory;

internal sealed class InventoryLedgerConfiguration : IEntityTypeConfiguration<InventoryLedger>
{
    public void Configure(EntityTypeBuilder<InventoryLedger> builder)
    {
        builder.ToTable("inventory_ledgers", "inventory", table =>
        {
            table.HasCheckConstraint(
                "CK_inventory_ledgers_quantity_bounds",
                "\"QuantityBefore\" >= 0 AND \"QuantityAfter\" >= 0");
            table.HasCheckConstraint(
                "CK_inventory_ledgers_price_bounds",
                "(\"UnitPriceBefore\" IS NULL OR \"UnitPriceBefore\" >= 0) AND (\"UnitPriceAfter\" IS NULL OR \"UnitPriceAfter\" >= 0)");
        });
        builder.HasKey(ledger => ledger.Id);
        builder.HasIndex(ledger => new { ledger.InventorySessionId, ledger.OccurredAt, ledger.Id });
        builder.HasIndex(ledger => new { ledger.DailyProductListingId, ledger.OccurredAt, ledger.Id });
        builder.Property(ledger => ledger.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(ledger => ledger.QuantityDelta).HasPrecision(18, 3).IsRequired();
        builder.Property(ledger => ledger.QuantityBefore).HasPrecision(18, 3).IsRequired();
        builder.Property(ledger => ledger.QuantityAfter).HasPrecision(18, 3).IsRequired();
        builder.Property(ledger => ledger.UnitPriceBefore).HasPrecision(18, 2);
        builder.Property(ledger => ledger.UnitPriceAfter).HasPrecision(18, 2);
        builder.Property(ledger => ledger.ReferenceType).HasMaxLength(64).IsRequired();
        builder.Property(ledger => ledger.Reason).HasMaxLength(500);
        builder.Property(ledger => ledger.OccurredAt).IsRequired();
        builder.Property(ledger => ledger.CreatedAt).IsRequired();
        builder.Property(ledger => ledger.CreatedBy);

        builder.HasOne(ledger => ledger.DailyProductListing)
            .WithMany(listing => listing.InventoryLedgers)
            .HasForeignKey(ledger => ledger.DailyProductListingId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(ledger => ledger.InventorySession)
            .WithMany(session => session.InventoryLedgers)
            .HasForeignKey(ledger => ledger.InventorySessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
