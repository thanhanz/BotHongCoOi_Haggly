namespace Haggly.Domain.Modules.Payments;

public enum PaymentStatus
{
    PENDING,
    PROCESSING,
    PAID,
    PARTIALLY_PAID,
    FAILED,
    REFUNDED,
    CANCELLED
}
