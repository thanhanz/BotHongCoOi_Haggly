using Haggly.Domain.Common;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Finance;

public sealed class RevenueLedger : ImmutableEntity
{
    public Guid StallFulfillmentId { get; set; }
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
}
