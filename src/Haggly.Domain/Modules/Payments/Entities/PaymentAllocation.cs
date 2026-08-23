using Haggly.Domain.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Payments;

public sealed class PaymentAllocation : ImmutableEntity
{
    private PaymentAllocation()
    {
    }

    public Guid PaymentTransactionId { get; private set; }
    public Guid StallFulfillmentId { get; private set; }
    public Guid StallId { get; private set; }
    public PaymentAllocationType AllocationType { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public DateTimeOffset AllocatedAt { get; private set; }

    public PaymentTransaction? PaymentTransaction { get; private set; }
    public StallFulfillment? StallFulfillment { get; private set; }

    public static PaymentAllocation CreateSale(
        Guid id,
        Guid paymentTransactionId,
        Guid stallFulfillmentId,
        Guid stallId,
        decimal allocatedAmount,
        DateTimeOffset allocatedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A valid allocation ID is required.", nameof(id));
        if (paymentTransactionId == Guid.Empty)
            throw new ArgumentException("A valid transaction ID is required.", nameof(paymentTransactionId));
        if (stallFulfillmentId == Guid.Empty)
            throw new ArgumentException("A valid fulfillment ID is required.", nameof(stallFulfillmentId));
        if (stallId == Guid.Empty)
            throw new ArgumentException("A valid stall ID is required.", nameof(stallId));
        if (allocatedAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(allocatedAmount), "Allocation amount must be positive.");

        var utcAllocatedAt = allocatedAt.ToUniversalTime();
        return new PaymentAllocation
        {
            Id = id,
            PaymentTransactionId = paymentTransactionId,
            StallFulfillmentId = stallFulfillmentId,
            StallId = stallId,
            AllocationType = PaymentAllocationType.PAYMENT,
            AllocatedAmount = allocatedAmount,
            AllocatedAt = utcAllocatedAt,
            CreatedAt = utcAllocatedAt
        };
    }
}
