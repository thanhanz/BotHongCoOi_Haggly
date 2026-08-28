using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Abstractions.Sales;

public interface IOrderCommandRepository
{
    Task<Order?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<Order?> FindForPaymentAsync(
        Guid orderId,
        CancellationToken cancellationToken)
        => FindByIdAsync(orderId, cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
