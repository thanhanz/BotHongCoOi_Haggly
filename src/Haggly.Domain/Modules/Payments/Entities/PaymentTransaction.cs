using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Payments;

public sealed class PaymentTransaction : AuditableEntity
{
    public Guid PaymentId { get; private set; }
    public PaymentTransactionType TransactionType { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentTransactionStatus Status { get; private set; } = PaymentTransactionStatus.PENDING;
    public string? ProviderResponseCode { get; private set; }
    public string? ProviderResponseData { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public Payment? Payment { get; private set; }
    public ICollection<PaymentAllocation> Allocations { get; private set; } = new List<PaymentAllocation>();

    private PaymentTransaction()
    {
    }
    
    public static PaymentTransaction Create(
        Guid id,
        Payment payment,
        decimal amount,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A valid payment transaction ID is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(payment);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Transaction amount must be positive.");
        if (amount != payment.AmountDue)
            throw new InvalidOperationException("The MVP transaction amount must equal the payment amount due.");

        var utcCreatedAt = createdAt.ToUniversalTime();

        return new PaymentTransaction
        {
            Id = id,
            PaymentId = payment.Id,
            Payment = payment,
            TransactionType = PaymentTransactionType.PAYMENT,
            Amount = amount,
            Status = PaymentTransactionStatus.PENDING,
            CreatedAt = utcCreatedAt
        };
    }

    public void MarkSucceeded(
        string providerTransactionId,
        string? providerResponseCode,
        string? providerResponseData,
        DateTimeOffset processedAt)
    {
        EnsurePending();
        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);

        var utcProcessedAt = processedAt.ToUniversalTime();
        Status = PaymentTransactionStatus.SUCCEEDED;
        ProviderTransactionId = providerTransactionId.Trim();
        ProviderResponseCode = Normalize(providerResponseCode);
        ProviderResponseData = Normalize(providerResponseData);
        FailureReason = null;
        ProcessedAt = utcProcessedAt;
        UpdatedAt = utcProcessedAt;
    }

    public void MarkFailed(
        string failureReason,
        string? providerResponseCode,
        string? providerResponseData,
        DateTimeOffset processedAt)
    {
        EnsurePending();
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        var utcProcessedAt = processedAt.ToUniversalTime();
        Status = PaymentTransactionStatus.FAILED;
        ProviderResponseCode = Normalize(providerResponseCode);
        ProviderResponseData = Normalize(providerResponseData);
        FailureReason = failureReason.Trim();
        ProcessedAt = utcProcessedAt;
        UpdatedAt = utcProcessedAt;
    }

    private void EnsurePending()
    {
        if (Status != PaymentTransactionStatus.PENDING)
            throw new InvalidOperationException("Only a pending payment transaction can be completed.");
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
