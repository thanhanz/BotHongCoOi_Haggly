using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Queries;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.Handlers;

public sealed class CartApplicationHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 19, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetCart_ExistingLine_ReturnsStallProductInventoryAndNegotiationData()
    {
        var buyerId = Guid.NewGuid();
        var line = ReadLine();
        var handler = new GetCartHandler(new FakeCartQuery
        {
            Result = new CartReadModel(Guid.NewGuid(), buyerId, [line])
        });

        var result = await handler.Handle(new GetCartQuery(buyerId), CancellationToken.None);

        var item = Assert.Single(Assert.Single(result.Stalls).Items);
        Assert.Equal("Fresh Stall", result.Stalls[0].Stall.Name);
        Assert.Equal("Tomato", item.Product.Name);
        Assert.Equal("Ripe tomatoes", item.Product.Description);
        Assert.Equal(7m, item.RemainingQuantity);
        Assert.True(item.Offering.IsNegotiable);
    }

    [Fact]
    public async Task AddCartItem_QuantityExceedsRemainingQuantity_ThrowsValidationException()
    {
        var inventoryItemId = Guid.NewGuid();
        var cart = Cart.Create(Guid.NewGuid(), Now);
        var handler = new AddCartItemHandler(
            new FakeCartCommandRepository { Cart = cart },
            new FakeCartCatalog
            {
                Snapshots = [Snapshot(inventoryItemId, remainingQuantity: 2m)]
            },
            new FakeCartQuery(),
            new FixedClock());

        await Assert.ThrowsAsync<CartValidationException>(() => handler.Handle(
            new AddCartItemCommand(cart.BuyerId, inventoryItemId, 3m, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task AddCartItem_ValidQuantity_CreatesAndSavesBuyerCart()
    {
        var buyerId = Guid.NewGuid();
        var inventoryItemId = Guid.NewGuid();
        var repository = new FakeCartCommandRepository();
        var handler = new AddCartItemHandler(
            repository,
            new FakeCartCatalog { Snapshots = [Snapshot(inventoryItemId, 4m)] },
            new FakeCartQuery(),
            new FixedClock());

        await handler.Handle(
            new AddCartItemCommand(buyerId, inventoryItemId, 2m, "Ripe"),
            CancellationToken.None);

        Assert.Equal(1, repository.AddCalls);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task AddCartItem_QuantityBelowMinimum_ThrowsValidationException()
    {
        var inventoryItemId = Guid.NewGuid();
        var cart = Cart.Create(Guid.NewGuid(), Now);
        var handler = new AddCartItemHandler(
            new FakeCartCommandRepository { Cart = cart },
            new FakeCartCatalog
            {
                Snapshots = [Snapshot(inventoryItemId, 4m) with { MinimumOrderQuantity = 3m }]
            },
            new FakeCartQuery(),
            new FixedClock());

        await Assert.ThrowsAsync<CartValidationException>(() => handler.Handle(
            new AddCartItemCommand(cart.BuyerId, inventoryItemId, 2m, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCartItem_QuantityExceedsRemainingQuantity_ThrowsValidationException()
    {
        var inventoryItemId = Guid.NewGuid();
        var cart = Cart.Create(Guid.NewGuid(), Now);
        var item = cart.AddItem(inventoryItemId, 1m, null, Now);
        var handler = new UpdateCartItemHandler(
            new FakeCartCommandRepository { Cart = cart },
            new FakeCartCatalog
            {
                Snapshots = [Snapshot(inventoryItemId, remainingQuantity: 1.5m)]
            },
            new FakeCartQuery(),
            new FixedClock());

        await Assert.ThrowsAsync<CartValidationException>(() => handler.Handle(
            new UpdateCartItemCommand(cart.BuyerId, item.Id, 2m, null),
            CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCartItem_ValidQuantity_UpdatesQuantityAndNotes()
    {
        var inventoryItemId = Guid.NewGuid();
        var cart = Cart.Create(Guid.NewGuid(), Now);
        var item = cart.AddItem(inventoryItemId, 1m, null, Now);
        var handler = new UpdateCartItemHandler(
            new FakeCartCommandRepository { Cart = cart },
            new FakeCartCatalog { Snapshots = [Snapshot(inventoryItemId, 5m)] },
            new FakeCartQuery(),
            new FixedClock());

        await handler.Handle(
            new UpdateCartItemCommand(cart.BuyerId, item.Id, 3m, "Updated note"),
            CancellationToken.None);

        Assert.Equal(3m, item.Quantity);
        Assert.Equal("Updated note", item.Notes);
    }

    [Fact]
    public async Task RemoveCartItem_OwnedItem_RemovesItemAndSaves()
    {
        var cart = Cart.Create(Guid.NewGuid(), Now);
        var item = cart.AddItem(Guid.NewGuid(), 1m, null, Now);
        var repository = new FakeCartCommandRepository { Cart = cart };
        var handler = new RemoveCartItemHandler(repository, new FakeCartQuery(), new FixedClock());

        await handler.Handle(
            new RemoveCartItemCommand(cart.BuyerId, item.Id),
            CancellationToken.None);

        Assert.Empty(cart.Items);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task ClearCart_ExistingItems_RemovesAllItemsAndSaves()
    {
        var cart = Cart.Create(Guid.NewGuid(), Now);
        cart.AddItem(Guid.NewGuid(), 1m, null, Now);
        cart.AddItem(Guid.NewGuid(), 1m, null, Now);
        var repository = new FakeCartCommandRepository { Cart = cart };
        var handler = new ClearCartHandler(repository, new FakeCartQuery(), new FixedClock());

        await handler.Handle(new ClearCartCommand(cart.BuyerId), CancellationToken.None);

        Assert.Empty(cart.Items);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task CheckoutCart_ValidMultiStallCart_CreatesNegotiatingOrderAndClearsCart()
    {
        var buyerId = Guid.NewGuid();
        var firstItemId = Guid.NewGuid();
        var secondItemId = Guid.NewGuid();
        var cart = Cart.Create(buyerId, Now);
        cart.AddItem(firstItemId, 2m, null, Now);
        cart.AddItem(secondItemId, 1m, "Cleaned", Now);
        var orders = new FakeOrderCommandRepository();
        var repository = new FakeCartCommandRepository { Cart = cart };
        var handler = new CheckoutCartHandler(
            repository,
            new FakeCartCatalog
            {
                Snapshots =
                [
                    Snapshot(firstItemId, remainingQuantity: 5m, stallId: Guid.NewGuid(), productName: "Tomato", unitPrice: 45_000m),
                    Snapshot(secondItemId, remainingQuantity: 5m, stallId: Guid.NewGuid(), productName: "Fish", unitPrice: 120_000m)
                ]
            },
            orders,
            new InlineCheckoutUnitOfWork(),
            new FixedClock());

        var result = await handler.Handle(new CheckoutCartCommand(buyerId), CancellationToken.None);

        Assert.Equal(OrderStatus.NEGOTIATING, result.Status);
        Assert.Equal(2, result.Fulfillments.Count);
        Assert.Empty(cart.Items);
        Assert.Single(orders.Orders);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task CheckoutCart_InventoryDroppedBelowQuantity_DoesNotCreateOrderOrClearCart()
    {
        var buyerId = Guid.NewGuid();
        var inventoryItemId = Guid.NewGuid();
        var cart = Cart.Create(buyerId, Now);
        cart.AddItem(inventoryItemId, 3m, null, Now);
        var repository = new FakeCartCommandRepository { Cart = cart };
        var orders = new FakeOrderCommandRepository();
        var handler = new CheckoutCartHandler(
            repository,
            new FakeCartCatalog { Snapshots = [Snapshot(inventoryItemId, 2m)] },
            orders,
            new InlineCheckoutUnitOfWork(),
            new FixedClock());

        await Assert.ThrowsAsync<CartValidationException>(() => handler.Handle(
            new CheckoutCartCommand(buyerId),
            CancellationToken.None));

        Assert.Single(cart.Items);
        Assert.Empty(orders.Orders);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task CheckoutCart_EmptyCart_ThrowsValidationException()
    {
        var buyerId = Guid.NewGuid();
        var cart = Cart.Create(buyerId, Now);
        var handler = new CheckoutCartHandler(
            new FakeCartCommandRepository { Cart = cart },
            new FakeCartCatalog(),
            new FakeOrderCommandRepository(),
            new InlineCheckoutUnitOfWork(),
            new FixedClock());

        await Assert.ThrowsAsync<CartValidationException>(() => handler.Handle(
            new CheckoutCartCommand(buyerId),
            CancellationToken.None));
    }

    private static CartLineReadModel ReadLine()
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2m,
            "Please choose ripe ones",
            new CartStallReadModel(Guid.NewGuid(), Guid.NewGuid(), "F-01", "Fresh Stall", "Near gate", "0123"),
            new CartProductReadModel(Guid.NewGuid(), Guid.NewGuid(), "Tomato", "Ripe tomatoes", "tomato.jpg"),
            new CartOfferingReadModel("Tomato", ProductUnit.KG, 1m, 45_000m, true),
            7m);

    private static CartItemSnapshot Snapshot(
        Guid inventoryItemId,
        decimal remainingQuantity,
        Guid? stallId = null,
        string productName = "Tomato",
        decimal unitPrice = 45_000m)
        => new(
            inventoryItemId,
            Guid.NewGuid(),
            stallId ?? Guid.NewGuid(),
            productName,
            ProductUnit.KG,
            1m,
            unitPrice,
            true,
            remainingQuantity,
            true);

    private sealed class FixedClock : IBusinessClock
    {
        public DateTimeOffset GetNow() => Now;
        public DateOnly GetBusinessDate() => DateOnly.FromDateTime(Now.DateTime);
    }

    private sealed class FakeCartCatalog : ICartCatalog
    {
        public IReadOnlyList<CartItemSnapshot> Snapshots { get; init; } = [];

        public Task<IReadOnlyList<CartItemSnapshot>> GetItemsAsync(
            IReadOnlyCollection<Guid> inventoryItemIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CartItemSnapshot>>(
                Snapshots.Where(item => inventoryItemIds.Contains(item.InventoryItemId)).ToArray());
    }

    private sealed class FakeCartCommandRepository : ICartCommandRepository
    {
        public Cart? Cart { get; init; }
        public int AddCalls { get; private set; }
        public int SaveCalls { get; private set; }

        public Task<Cart?> FindByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken)
            => Task.FromResult(Cart?.BuyerId == buyerId ? Cart : null);

        public Task AddAsync(Cart cart, CancellationToken cancellationToken)
        {
            AddCalls++;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCartQuery : ICartQuery
    {
        public CartReadModel? Result { get; init; }

        public Task<CartReadModel?> GetAsync(Guid buyerId, CancellationToken cancellationToken)
            => Task.FromResult(Result?.BuyerId == buyerId ? Result : null);
    }

    private sealed class FakeOrderCommandRepository : IOrderCommandRepository
    {
        public List<Order> Orders { get; } = [];

        public Task<Order?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken)
            => Task.FromResult<Order?>(Orders.SingleOrDefault(order => order.Id == orderId));

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            Orders.Add(order);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class InlineCheckoutUnitOfWork : ICartCheckoutUnitOfWork
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
            => operation(cancellationToken);
    }
}
