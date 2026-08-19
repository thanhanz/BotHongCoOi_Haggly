using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Queries;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.Handlers;

public sealed class OrderApplicationHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateOrder_WithLinesFromMultipleStalls_PersistsOneBuyerOrder()
    {
        var buyerId = Guid.NewGuid();
        var firstItemId = Guid.NewGuid();
        var secondItemId = Guid.NewGuid();
        var catalog = new FakeOrderCatalog
        {
            Snapshots =
            [
                new OrderLineSnapshot(firstItemId, Guid.NewGuid(), "Tomato", ProductUnit.KG, 45_000m, 5m),
                new OrderLineSnapshot(secondItemId, Guid.NewGuid(), "Fish", ProductUnit.KG, 120_000m, 5m)
            ]
        };
        var repository = new FakeOrderCommandRepository();
        var handler = new CreateOrderHandler(repository, catalog, new FixedBusinessClock());

        var result = await handler.Handle(
            new CreateOrderCommand(
                buyerId,
                [new CreateOrderLine(firstItemId, 2m, null), new CreateOrderLine(secondItemId, 1m, "Cleaned")]),
            CancellationToken.None);

        Assert.Equal(buyerId, result.BuyerId);
        Assert.Equal(OrderStatus.NEGOTIATING, result.Status);
        Assert.Equal(2, result.Fulfillments.Count);
        Assert.Single(repository.Orders);
    }

    [Fact]
    public async Task CreateOrder_WhenRequestedQuantityExceedsAvailable_ThrowsValidationException()
    {
        var itemId = Guid.NewGuid();
        var catalog = new FakeOrderCatalog
        {
            Snapshots =
            [new OrderLineSnapshot(itemId, Guid.NewGuid(), "Tomato", ProductUnit.KG, 45_000m, 1m)]
        };
        var handler = new CreateOrderHandler(
            new FakeOrderCommandRepository(), catalog, new FixedBusinessClock());

        await Assert.ThrowsAsync<OrderValidationException>(() => handler.Handle(
            new CreateOrderCommand(
                Guid.NewGuid(), [new CreateOrderLine(itemId, 2m, null)]),
            CancellationToken.None));
    }

    [Fact]
    public async Task GetOrderDetails_WhenBuyerDoesNotOwnOrder_ThrowsForbiddenException()
    {
        var order = CreateOrder(Guid.NewGuid());
        var query = new FakeOrderQuery { Order = order };
        var handler = new GetOrderDetailsHandler(query);

        await Assert.ThrowsAsync<OrderForbiddenException>(() => handler.Handle(
            new GetOrderDetailsQuery(order.Id, Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task CancelOrder_WhenBuyerOwnsOrder_CancelsAndSavesIt()
    {
        var buyerId = Guid.NewGuid();
        var order = CreateOrder(buyerId);
        var repository = new FakeOrderCommandRepository { Existing = order };
        var handler = new CancelOrderHandler(repository, new FixedBusinessClock());

        var result = await handler.Handle(
            new CancelOrderCommand(order.Id, buyerId, "Changed my mind"),
            CancellationToken.None);

        Assert.Equal(OrderStatus.CANCELLED, result.Status);
        Assert.Equal(1, repository.SaveCalls);
    }

    private static Order CreateOrder(Guid buyerId)
        => Order.Place(
            Guid.NewGuid(),
            buyerId,
            [new OrderItemInput(
                Guid.NewGuid(), Guid.NewGuid(), "Tomato", ProductUnit.KG, 45_000m, 1m, null)],
            Now);

    private sealed class FixedBusinessClock : IBusinessClock
    {
        public DateTimeOffset GetNow() => Now;
        public DateOnly GetBusinessDate() => DateOnly.FromDateTime(Now.DateTime);
    }

    private sealed class FakeOrderCatalog : IOrderCatalog
    {
        public IReadOnlyList<OrderLineSnapshot> Snapshots { get; init; } = [];

        public Task<IReadOnlyList<OrderLineSnapshot>> GetOrderLinesAsync(
            IReadOnlyCollection<Guid> inventoryItemIds,
            CancellationToken cancellationToken)
            => Task.FromResult(Snapshots);
    }

    private sealed class FakeOrderCommandRepository : IOrderCommandRepository
    {
        public Order? Existing { get; init; }
        public List<Order> Orders { get; } = [];
        public int SaveCalls { get; private set; }

        public Task<Order?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken)
            => Task.FromResult(Existing?.Id == orderId ? Existing : null);

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            Orders.Add(order);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOrderQuery : IOrderQuery
    {
        public Order? Order { get; init; }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
            => Task.FromResult(Order?.Id == orderId ? Order : null);

        public Task<PagedResult<Order>> GetPageAsync(
            Guid buyerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
            => Task.FromResult(new PagedResult<Order>(
                Order is not null && Order.BuyerId == buyerId ? [Order] : [],
                page,
                pageSize,
                Order is not null && Order.BuyerId == buyerId ? 1 : 0));
    }
}
