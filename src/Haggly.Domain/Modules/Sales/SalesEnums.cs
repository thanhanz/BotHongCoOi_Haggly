namespace Haggly.Domain.Modules.Sales;

public enum OrderStatus
{
    Draft,
    Negotiating,
    Agreed,
    PaymentPending,
    Paid,
    PartiallyPickedUp,
    Completed,
    Cancelled
}

public enum StallFulfillmentStatus
{
    Draft,
    Negotiating,
    Agreed,
    Preparing,
    Ready,
    PickedUp,
    Cancelled
}

public enum OrderItemStatus
{
    Active,
    Cancelled,
    Refunded
}
