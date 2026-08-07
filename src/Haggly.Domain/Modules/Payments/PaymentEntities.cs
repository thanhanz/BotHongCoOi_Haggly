using Haggly.Domain.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Payments;

public sealed class PaymentMethod : SoftDeletableEntity
{
    public PaymentMethodCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public bool IsOnline { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ConfigurationJson { get; set; }
}

public sealed class Payment : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string PaymentNo { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTimeOffset InitiatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public Order? Order { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}

public sealed class PaymentTransaction : AuditableEntity
{
    public Guid PaymentId { get; set; }
    public PaymentTransactionType TransactionType { get; set; }
    public string? ProviderTransactionId { get; set; }
    public decimal Amount { get; set; }
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;
    public string? ProviderResponseCode { get; set; }
    public string? ProviderResponseData { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }

    public Payment? Payment { get; set; }
    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}

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
