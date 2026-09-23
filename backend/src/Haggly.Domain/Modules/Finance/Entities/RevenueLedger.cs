using Haggly.Domain.Common;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Finance;

public sealed class RevenueLedger : ImmutableEntity
{
    public Guid StallId { get; private set; }
    public Guid? StallFulfillmentId { get; private set; }
    public Guid? PosSaleId { get; private set; }
    public Guid? PaymentAllocationId { get; private set; }
    public RevenueEntryType EntryType { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal RefundAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid? ReferenceId { get; private set; }
    public string? Notes { get; private set; }

    public StallFulfillment? StallFulfillment { get; private set; }
    public PaymentAllocation? PaymentAllocation { get; private set; }

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

        if (grossAmount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(grossAmount));
        }

        var utcOccurredAt = occurredAt.ToUniversalTime();
        return new RevenueLedger
        {
            StallId = stallId,
            PosSaleId = saleId,
            EntryType = RevenueEntryType.SALE,
            GrossAmount = grossAmount,
            RefundAmount = 0m,
            NetAmount = grossAmount,
            OccurredAt = utcOccurredAt,
            ReferenceType = "POS_SALE",
            ReferenceId = saleId,
            CreatedAt = utcOccurredAt
        };
    }

    public static RevenueLedger CreatePaymentSaleEntry(
        Guid paymentAllocationId,
        Guid stallFulfillmentId,
        Guid stallId,
        decimal grossAmount,
        DateTimeOffset occurredAt)
    {
        if (paymentAllocationId == Guid.Empty
            || stallFulfillmentId == Guid.Empty
            || stallId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid allocation, fulfillment, and stall IDs are required.");
        }
        if (grossAmount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(grossAmount), "Revenue amount must be positive.");

        var utcOccurredAt = occurredAt.ToUniversalTime();
        return new RevenueLedger
        {
            PaymentAllocationId = paymentAllocationId,
            StallFulfillmentId = stallFulfillmentId,
            StallId = stallId,
            EntryType = RevenueEntryType.SALE,
            GrossAmount = grossAmount,
            RefundAmount = 0m,
            NetAmount = grossAmount,
            OccurredAt = utcOccurredAt,
            ReferenceType = "PAYMENT_ALLOCATION",
            ReferenceId = paymentAllocationId,
            CreatedAt = utcOccurredAt
        };
    }
}
