using Haggly.Domain.Modules.Payments;

namespace Haggly.Application.Abstractions.Payments;

public interface IPaymentCommandRepository
{
    Task<Payment?> FindByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken);

    Task<Payment?> FindByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task AddAsync(Payment payment, CancellationToken cancellationToken);

    Task AddTransactionAsync(
        PaymentTransaction transaction,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
