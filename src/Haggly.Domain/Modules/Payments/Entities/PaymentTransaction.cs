using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Payments;

public sealed class PaymentTransaction : AuditableEntity
{
    public Guid PaymentId { get; set; }
    public PaymentTransactionType TransactionType { get; set; }
    public string? ProviderTransactionId { get; set; }
    public decimal Amount { get; set; }
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.PENDING;
    public string? ProviderResponseCode { get; set; }
    public string? ProviderResponseData { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }

    public Payment? Payment { get; set; }
    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}
