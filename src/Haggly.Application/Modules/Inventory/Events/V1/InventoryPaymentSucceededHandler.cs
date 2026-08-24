using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Modules.Inventory;

namespace Haggly.Application.Modules.Inventory.Events.V1;

public sealed class InventoryPaymentSucceededHandler(
    IInventoryPaymentRepository repository)
    : IEventHandler<PaymentSucceededEvent>
{
    public async Task HandleAsync(
        PaymentSucceededEvent message,
        CancellationToken cancellationToken)
    {
      var transactionType = InventoryTransactionType.ONLINE_SALE;  
      
        if (await repository.HasProcessedAsync(
                message.PaymentTransactionId,
                transactionType,
                cancellationToken))
        {
            return;
        }

        var orderItems = await repository.FindActiveOrderItemsAsync(
            message.OrderId,
            cancellationToken);
        if (orderItems.Count == 0)
            throw new InvalidOperationException("The paid order has no active inventory items.");

        foreach (var orderItem in orderItems)
        {
            var inventoryItem = orderItem.InventoryItem
                ?? throw new InvalidOperationException($"Inventory item '{orderItem.InventoryItemId}' was not found.");
            
            inventoryItem.RecordOnlineSale(
                orderItem.FinalQuantity,
                message.PaymentTransactionId,
                message.OccurredAt);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
