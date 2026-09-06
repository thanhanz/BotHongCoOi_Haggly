using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Sales.Exceptions;

namespace Haggly.Application.Modules.Sales.Events.V1;

public sealed class OrderPaymentFailedHandler(
    IOrderCommandRepository orderRepository,
    IInboxRepository inboxRepository,
    ISalesTransactionExecutor transactionExecutor,
    IBusinessClock businessClock) : IEventHandler<PaymentFailedEvent>
{
    private const string ConsumerName = "order-payment-failed-v1";
    private const string EventType = "payments.payment-failed.v1";

    public async Task HandleAsync(
        PaymentFailedEvent message,
        CancellationToken cancellationToken)
    {
        await transactionExecutor.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var consumed = await inboxRepository.TryAddAsync(
                ConsumerName,
                message.EventId,
                EventType,
                businessClock.GetNow().ToUniversalTime(),
                transactionCancellationToken);
            if (!consumed)
                return false;

            var order = await orderRepository.FindByIdAsync(
                message.OrderId,
                transactionCancellationToken)
                ?? throw new OrderNotFoundException(
                    $"Order '{message.OrderId}' was not found.");

            if (message.Amount != order.TotalToCharge
                || !string.Equals(
                    message.Currency,
                    order.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The failed payment amount and currency must match the order.");
            }

            var changed = order.ApplyFailedPayment(message.OccurredAt);
            if (changed)
                await orderRepository.SaveChangesAsync(transactionCancellationToken);

            return changed;
        }, cancellationToken);
    }
}
