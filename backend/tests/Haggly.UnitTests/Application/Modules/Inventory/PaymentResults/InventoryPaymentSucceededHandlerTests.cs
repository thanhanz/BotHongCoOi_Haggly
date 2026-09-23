using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Modules.Inventory.Events.V1;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Inventory;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.UnitTests.Application.Modules.Inventory.PaymentResults;

public sealed class InventoryPaymentSucceededHandlerTests
{
    private readonly IInventoryPaymentRepository _repository = Substitute.For<IInventoryPaymentRepository>();

    [Fact]
    public async Task HandleAsync_PaymentHasActiveReservedItems_ConsumesReservedInventory()
    {
        // Arrange
        var fixture = CreateFixture();
        _repository.HasProcessedAsync(Arg.Any<Guid>(), InventoryTransactionType.ONLINE_SALE, Arg.Any<CancellationToken>()).Returns(false);
        _repository.FindActiveOrderItemsAsync(fixture.Order.Id, Arg.Any<CancellationToken>()).Returns([fixture.OrderItem]);
        var message = CreateMessage(fixture.Order.Id);

        // Act
        await CreateSubject().HandleAsync(message, CancellationToken.None);

        // Assert
        Assert.Equal(7m, fixture.InventoryItem.CurrentQuantity);
        Assert.Equal(0m, fixture.InventoryItem.ReservedQuantity);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PaymentWasAlreadyProcessed_DoesNotConsumeOrSave()
    {
        // Arrange
        var fixture = CreateFixture();
        _repository.HasProcessedAsync(Arg.Any<Guid>(), InventoryTransactionType.ONLINE_SALE, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await CreateSubject().HandleAsync(CreateMessage(fixture.Order.Id), CancellationToken.None);

        // Assert
        await _repository.DidNotReceive().FindActiveOrderItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private InventoryPaymentSucceededHandler CreateSubject() => new(_repository);

    private static PaymentSucceededEvent CreateMessage(Guid orderId)
        => new(Guid.Parse("E0000000-0000-0000-0000-000000000001"), Guid.Parse("E0000000-0000-0000-0000-000000000002"), Now, Guid.Parse("E0000000-0000-0000-0000-000000000003"), Guid.Parse("E0000000-0000-0000-0000-000000000004"), orderId, 150_000m, "VND", "SIM-1", [Guid.Parse("E0000000-0000-0000-0000-000000000005")]);

    private static InventoryFixture CreateFixture()
    {
        var ownerId = Guid.Parse("E1000000-0000-0000-0000-000000000001");
        var inventory = DomainInventory.Create(Guid.Parse("E1000000-0000-0000-0000-000000000002"), ownerId, Now);
        var inventoryItem = inventory.AddItem(Guid.Parse("E1000000-0000-0000-0000-000000000003"), 10m, ownerId, Now);
        var order = Order.Place(Guid.Parse("E1000000-0000-0000-0000-000000000004"), ownerId, [new OrderItemInput(inventoryItem.Id, inventory.StallId, "Rice", ProductUnit.KG, 50_000m, 3m, null)], Now);
        var orderItem = Assert.Single(Assert.Single(order.StallFulfillments).OrderItems);
        orderItem.InventoryItem = inventoryItem;
        inventoryItem.Reserve(3m, Now);
        return new InventoryFixture(inventoryItem, order, orderItem);
    }

    private static readonly DateTimeOffset Now = new(2026, 8, 30, 13, 0, 0, TimeSpan.Zero);
    private sealed record InventoryFixture(InventoryItem InventoryItem, Order Order, OrderItem OrderItem);
}
