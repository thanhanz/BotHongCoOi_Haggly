using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Inventory.Events.V1;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Application.Modules.Inventory;

public sealed class InventoryPaymentSucceededHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 23, 9, 0, 0, TimeSpan.FromHours(7));

    [Fact]
    public async Task HandleAsync_WhenOrderHasActiveItems_RecordsOnlineSalesOnce()
    {
        var inventory = DomainInventory.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        var inventoryItem = inventory.AddItem(Guid.NewGuid(), 10m, Guid.NewGuid(), Now);
        var order = Order.Place(Guid.NewGuid(), Guid.NewGuid(),
        [
            new OrderItemInput(
                inventoryItem.Id,
                inventory.StallId,
                "Rice",
                ProductUnit.KG,
                50_000m,
                3m,
                null)
        ], Now);
        var orderItem = Assert.Single(Assert.Single(order.StallFulfillments).OrderItems);
        orderItem.InventoryItem = inventoryItem;
        var repository = new FakeInventoryPaymentRepository([orderItem]);
        var handler = new InventoryPaymentSucceededHandler(repository);
        var message = CreateEvent(order.Id);

        await handler.HandleAsync(message, CancellationToken.None);
        await handler.HandleAsync(message, CancellationToken.None);

        Assert.Equal(7m, inventoryItem.CurrentQuantity);
        Assert.Equal(1, repository.SaveCount);
        Assert.Contains(inventoryItem.InventoryLedgers, ledger =>
            ledger.TransactionType == InventoryTransactionType.ONLINE_SALE
            && ledger.ReferenceId == message.PaymentTransactionId);
    }

    private static PaymentSucceededEvent CreateEvent(Guid orderId)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            Guid.NewGuid(),
            Guid.NewGuid(),
            orderId,
            150_000m,
            "VND",
            "SIM-1",
            [Guid.NewGuid()]);

    private sealed class FakeInventoryPaymentRepository(IReadOnlyList<OrderItem> items)
        : IInventoryPaymentRepository
    {
        public HashSet<Guid> ProcessedPaymentTransactionIds { get; } = [];
        public int SaveCount { get; private set; }

        public Task<bool> HasProcessedAsync(
            Guid paymentTransactionId,
            InventoryTransactionType transactionType,
            CancellationToken cancellationToken)
            => Task.FromResult(
                transactionType == InventoryTransactionType.ONLINE_SALE
                && ProcessedPaymentTransactionIds.Contains(paymentTransactionId));

        public Task<IReadOnlyList<OrderItem>> FindActiveOrderItemsAsync(
            Guid orderId,
            CancellationToken cancellationToken)
            => Task.FromResult(items);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            foreach (var paymentTransactionId in items
                         .SelectMany(item => item.InventoryItem!.InventoryLedgers)
                         .Where(ledger => ledger.TransactionType == InventoryTransactionType.ONLINE_SALE)
                         .Select(ledger => ledger.ReferenceId!.Value))
            {
                ProcessedPaymentTransactionIds.Add(paymentTransactionId);
            }
            return Task.CompletedTask;
        }
    }
}
