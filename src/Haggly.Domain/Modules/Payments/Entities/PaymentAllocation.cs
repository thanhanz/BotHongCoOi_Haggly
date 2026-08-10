using Haggly.Domain.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Payments;

public sealed class PaymentAllocation : ImmutableEntity
{
    public Guid PaymentTransactionId { get; set; }
    public Guid StallFulfillmentId { get; set; }
    public PaymentAllocationType AllocationType { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTimeOffset AllocatedAt { get; set; } = DateTimeOffset.UtcNow;

    public PaymentTransaction? PaymentTransaction { get; set; }
    public StallFulfillment? StallFulfillment { get; set; }
}
