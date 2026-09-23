using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Modules.Sales.Events.V1;

public sealed class OrderPaymentSucceededHandler(
    IOrderCommandRepository orderRepository,
    IPaymentAllocationRepository allocationRepository) : IEventHandler<PaymentSucceededEvent>
{
    public async Task HandleAsync(
        PaymentSucceededEvent message,
        CancellationToken cancellationToken)
    {
        if (message.PaymentAllocationIds.Count == 0
            || message.PaymentAllocationIds.Distinct().Count()
            != message.PaymentAllocationIds.Count)
        {
            throw new InvalidOperationException(
                "PaymentSucceededEvent must reference unique payment allocations.");
        }

        var order = await orderRepository.FindByIdAsync(
            message.OrderId,
            cancellationToken)
            ?? throw new OrderNotFoundException($"Order '{message.OrderId}' was not found.");

        if (message.Amount != order.TotalToCharge
            || !string.Equals(message.Currency, order.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The successful payment amount and currency must match the order.");
        }

        var allocations = await allocationRepository.FindByIdsAsync(
            message.PaymentAllocationIds,
            cancellationToken);
            
        if (allocations.Count != message.PaymentAllocationIds.Count
            || allocations.Any(allocation =>
                allocation.PaymentTransactionId != message.PaymentTransactionId)
            || allocations.Sum(allocation => allocation.AllocatedAmount) != message.Amount)
        {
            throw new InvalidOperationException(
                "Payment allocations must belong to the successful transaction and equal its amount.");
        }

        var changed = order.ApplySuccessfulPayment(
            allocations.Select(allocation => new OrderPaymentAllocation(
                allocation.StallFulfillmentId,
                allocation.StallId,
                allocation.AllocatedAmount)).ToArray(),
            message.OccurredAt);

        if (changed)
            await orderRepository.SaveChangesAsync(cancellationToken);
    }
}
