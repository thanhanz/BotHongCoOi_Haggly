using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Abstractions.Payments;

public interface IPaymentCommandRepository
{
    Task<PaymentOrderSnapshot?> FindOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task<Payment?> FindByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task AddAsync(Payment payment, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record PaymentOrderSnapshot(
    Guid OrderId,
    OrderStatus Status,
    decimal Amount,
    string Currency);
