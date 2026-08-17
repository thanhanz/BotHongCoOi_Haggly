using Haggly.Domain.Common;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Finance;

public sealed class RevenueLedger : ImmutableEntity
{
    public Guid StallId { get; private set; }
    public Guid? StallFulfillmentId { get; private set; }
    public Guid? PosSaleId { get; private set; }
    public Guid? PaymentAllocationId { get; set; }
    public RevenueEntryType EntryType { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal NetAmount { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string ReferenceType { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }

    public StallFulfillment? StallFulfillment { get; set; }
    public PaymentAllocation? PaymentAllocation { get; set; }

    public static RevenueLedger CreatePosSaleEntry(
        Guid saleId,
        Guid stallId,
        decimal grossAmount,
        DateTimeOffset occurredAt)
    {
        if (saleId == Guid.Empty || stallId == Guid.Empty)
        {
            throw new ArgumentException("Valid sale and stall IDs are required.");
        }

        if (grossAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(grossAmount));
        }

        return new RevenueLedger
        {
            StallId = stallId,
            PosSaleId = saleId,
            EntryType = RevenueEntryType.SALE,
            GrossAmount = grossAmount,
            RefundAmount = 0m,
            NetAmount = grossAmount,
            OccurredAt = occurredAt,
            ReferenceType = "POS_SALE",
            ReferenceId = saleId,
            CreatedAt = occurredAt
        };
    }
}
