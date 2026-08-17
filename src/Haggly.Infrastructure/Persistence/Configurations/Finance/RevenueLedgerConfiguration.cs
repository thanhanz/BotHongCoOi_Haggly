using Haggly.Domain.Modules.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haggly.Infrastructure.Persistence.Configurations.Finance;

internal sealed class RevenueLedgerConfiguration : IEntityTypeConfiguration<RevenueLedger>
{
    public void Configure(EntityTypeBuilder<RevenueLedger> builder)
    {
        builder.ToTable("revenue_ledgers", "finance", table =>
        {
            table.HasCheckConstraint("CK_revenue_ledgers_amount_bounds",
                "\"GrossAmount\" >= 0 AND \"RefundAmount\" >= 0 AND \"NetAmount\" = \"GrossAmount\" - \"RefundAmount\"");
        });
        builder.HasKey(ledger => ledger.Id);
        builder.HasIndex(ledger => new { ledger.StallId, ledger.OccurredAt, ledger.Id });
        builder.HasIndex(ledger => new { ledger.PosSaleId, ledger.EntryType })
            .IsUnique()
            .HasFilter("\"PosSaleId\" IS NOT NULL");
        builder.Property(ledger => ledger.EntryType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(ledger => ledger.GrossAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(ledger => ledger.RefundAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(ledger => ledger.NetAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(ledger => ledger.ReferenceType).HasMaxLength(64).IsRequired();
        builder.Property(ledger => ledger.Notes).HasMaxLength(500);
        builder.Property(ledger => ledger.OccurredAt).IsRequired();
        builder.Property(ledger => ledger.CreatedAt).IsRequired();
        // Payment allocations and online fulfillments are separate future
        // workflows; POS revenue stores their identifiers without discovering
        // the unfinished payment/order graph.
        builder.Ignore(ledger => ledger.PaymentAllocation);
        builder.Ignore(ledger => ledger.StallFulfillment);
    }
}
