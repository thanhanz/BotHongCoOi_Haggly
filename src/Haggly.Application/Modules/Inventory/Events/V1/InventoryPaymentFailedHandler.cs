using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Events.V1;

namespace Haggly.Application.Modules.Inventory.Events.V1;

public sealed class InventoryPaymentFailedHandler(
    IInboxRepository inboxRepository,
    IInventoryPaymentRepository inventoryRepository,
    IInventoryUnitOfWork unitOfWork,
    IBusinessClock businessClock) : IEventHandler<PaymentFailedEvent>
{
    //TODO: Define these constants to a static class in the future for consistency
    private const string ConsumerName = "inventory-payment-failed-v1";
    private const string EventType = "payments.payment-failed.v1";

    public async Task HandleAsync(
        PaymentFailedEvent message,
        CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var added = await inboxRepository.TryAddAsync(
                ConsumerName,
                message.EventId,
                EventType,
                businessClock.GetNow().ToUniversalTime(),
                transactionCancellationToken);
            if (!added)
                return false;

            await inventoryRepository.ReleaseAsync(
                message.OrderId,
                message.OccurredAt,
                transactionCancellationToken);
            return true;
        }, cancellationToken);
    }
}
