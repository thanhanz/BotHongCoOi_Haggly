using Haggly.Domain.Modules.Payments;

namespace Haggly.Application.Modules.Payments.Dtos;

public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    string PaymentNo,
    decimal AmountDue,
    decimal AmountPaid,
    string Currency,
    PaymentStatus Status,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? CompletedAt)
{
    public static PaymentDto From(Payment payment)
        => new(
            payment.Id,
            payment.OrderId,
            payment.PaymentNo,
            payment.AmountDue,
            payment.AmountPaid,
            payment.Currency,
            payment.Status,
            payment.InitiatedAt,
            payment.CompletedAt);
}
