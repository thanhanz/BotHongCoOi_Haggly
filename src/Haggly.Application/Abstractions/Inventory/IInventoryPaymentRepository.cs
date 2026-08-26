using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Abstractions.Inventory;

public interface IInventoryPaymentRepository
{
    Task ReserveAsync(
        Guid orderId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        Guid orderId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);

    Task<bool> HasProcessedAsync(
        Guid paymentTransactionId,
        InventoryTransactionType transactionType,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderItem>> FindActiveOrderItemsAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
