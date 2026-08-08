namespace Haggly.Domain.Modules.Sales;

public enum OrderStatus
{
    DRAFT,
    NEGOTIATING,
    AGREED,
    PAYMENT_PENDING,
    PAID,
    PARTIALLY_PICKED_UP,
    COMPLETED,
    CANCELLED
}
