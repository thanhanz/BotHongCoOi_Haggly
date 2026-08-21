using Haggly.Domain.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Payments;

public sealed class Payment : AuditableEntity
{
    private Payment()
    {
    }

    public Guid OrderId { get; private set; }
    public Guid? PaymentMethodId { get; private set; }
    public string PaymentNo { get; private set; } = string.Empty;
    public decimal AmountDue { get; private set; }
    public decimal AmountPaid { get; private set; }
    public string Currency { get; private set; } = "VND";
    public PaymentStatus Status { get; private set; } = PaymentStatus.PENDING;
    public DateTimeOffset InitiatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public Order? Order { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public ICollection<PaymentTransaction> Transactions { get; private set; } = new List<PaymentTransaction>();

    public static Payment Create(
        Guid id,
        Guid orderId,
        decimal amountDue,
        string currency,
        DateTimeOffset initiatedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("A valid payment ID is required.", nameof(id));
        if (orderId == Guid.Empty)
            throw new ArgumentException("A valid order ID is required.", nameof(orderId));
        if (amountDue <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountDue), "Payment amount must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3)
            throw new ArgumentException("Currency must be a three-letter code.", nameof(currency));

        return new Payment
        {
            Id = id,
            OrderId = orderId,
            PaymentNo = $"PAY-{id:N}".ToUpperInvariant(),
            AmountDue = amountDue,
            AmountPaid = 0m,
            Currency = normalizedCurrency,
            Status = PaymentStatus.PENDING,
            InitiatedAt = initiatedAt,
            CreatedAt = initiatedAt
        };
    }

    public void StartProcessing(DateTimeOffset occurredAt)
    {
        if (Status is not PaymentStatus.PENDING and not PaymentStatus.FAILED)
            throw new InvalidOperationException("Only a pending or failed payment can start processing.");

        Status = PaymentStatus.PROCESSING;
        UpdatedAt = occurredAt;
    }

    public void MarkPaid(DateTimeOffset occurredAt)
    {
        if (Status != PaymentStatus.PROCESSING)
            throw new InvalidOperationException("Only a processing payment can be marked paid.");

        Status = PaymentStatus.PAID;
        AmountPaid = AmountDue;
        CompletedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    public void MarkFailed(DateTimeOffset occurredAt)
    {
        if (Status != PaymentStatus.PROCESSING)
            throw new InvalidOperationException("Only a processing payment can be marked failed.");

        Status = PaymentStatus.FAILED;
        AmountPaid = 0m;
        CompletedAt = null;
        UpdatedAt = occurredAt;
    }
}
