using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Dtos;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.CreateOrder;

public sealed class CreateOrderHandlerTests
{
    private readonly IOrderCommandRepository _repository = Substitute.For<IOrderCommandRepository>();

    private readonly IOrderCatalog _catalog = Substitute.For<IOrderCatalog>();

    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_ValidOrderLines_CreatesAndSavesOrder()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureCatalog(fixture);
        Order? createdOrder = null;
        _repository.AddAsync(
                Arg.Do<Order>(order => createdOrder = order),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var command = new CreateOrderCommand(
            fixture.BuyerId,
            [new CreateOrderLine(fixture.InventoryItemId, 2m, "  Fresh  ")]);

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(createdOrder);
        Assert.Equal(fixture.BuyerId, result.BuyerId);
        Assert.Equal(OrderStatus.NEGOTIATING, result.Status);
        Assert.Equal(90_000m, result.TotalToCharge);
        Assert.Equal("Fresh", Assert.Single(result.Fulfillments).Items[0].Notes);
        await _catalog.Received(1).GetOrderLinesAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { fixture.InventoryItemId })),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(
            Arg.Is<Order>(order =>
                order.BuyerId == fixture.BuyerId
                && order.TotalToCharge == 90_000m),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RequestedQuantityExceedsAvailability_ThrowsValidationException()
    {
        // Arrange
        var fixture = CreateFixture(availableQuantity: 1m);
        ConfigureCatalog(fixture);
        var command = new CreateOrderCommand(
            fixture.BuyerId,
            [new CreateOrderLine(fixture.InventoryItemId, 2m, null)]);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderValidationException>(action);
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CatalogOmitsRequestedItem_ThrowsValidationException()
    {
        // Arrange
        var fixture = CreateFixture();
        _catalog.GetOrderLinesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<OrderLineSnapshot>>([]);
        var command = new CreateOrderCommand(
            fixture.BuyerId,
            [new CreateOrderLine(fixture.InventoryItemId, 1m, null)]);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderValidationException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CatalogReturnsDuplicateItem_ThrowsValidationException()
    {
        // Arrange
        var fixture = CreateFixture();
        var snapshot = CreateSnapshot(fixture);
        _catalog.GetOrderLinesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<OrderLineSnapshot>>([snapshot, snapshot]);
        var command = new CreateOrderCommand(
            fixture.BuyerId,
            [new CreateOrderLine(fixture.InventoryItemId, 1m, null)]);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderValidationException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_InvalidQuantity_ThrowsValidationException(decimal quantity)
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CreateOrderCommand(
            fixture.BuyerId,
            [new CreateOrderLine(fixture.InventoryItemId, quantity, null)]);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderValidationException>(action);
        await _catalog.DidNotReceive().GetOrderLinesAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DomainRejectsInvalidSnapshot_PropagatesFailureWithoutSave()
    {
        // Arrange
        var fixture = CreateFixture();
        var invalidSnapshot = new OrderLineSnapshot(
            fixture.InventoryItemId,
            Guid.Empty,
            "Tomato",
            ProductUnit.KG,
            45_000m,
            2m);
        _catalog.GetOrderLinesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<OrderLineSnapshot>>([invalidSnapshot]);
        _clock.GetNow().Returns(fixture.Now);
        var command = new CreateOrderCommand(
            fixture.BuyerId,
            [new CreateOrderLine(fixture.InventoryItemId, 1m, null)]);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_ForwardsTokenToCatalog()
    {
        // Arrange
        var fixture = CreateFixture();
        var cancellationToken = new CancellationToken(canceled: true);
        _catalog.GetOrderLinesAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                cancellationToken)
            .Returns(Task.FromCanceled<IReadOnlyList<OrderLineSnapshot>>(cancellationToken));
        var command = new CreateOrderCommand(
            fixture.BuyerId,
            [new CreateOrderLine(fixture.InventoryItemId, 1m, null)]);

        // Act
        var action = () => CreateSubject().Handle(command, cancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _catalog.Received(1).GetOrderLinesAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { fixture.InventoryItemId })),
            cancellationToken);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private CreateOrderHandler CreateSubject()
        => new(_repository, _catalog, _clock);

    private void ConfigureCatalog(OrderFixture fixture)
    {
        _catalog.GetOrderLinesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<OrderLineSnapshot>>([CreateSnapshot(fixture)]);
        _clock.GetNow().Returns(fixture.Now);
    }

    private static OrderLineSnapshot CreateSnapshot(OrderFixture fixture)
        => new(fixture.InventoryItemId, fixture.StallId, "Tomato", ProductUnit.KG, 45_000m, fixture.AvailableQuantity);

    private static OrderFixture CreateFixture(decimal availableQuantity = 5m)
        => new(
            Guid.Parse("A0000000-0000-0000-0000-000000000001"),
            Guid.Parse("A0000000-0000-0000-0000-000000000002"),
            Guid.Parse("A0000000-0000-0000-0000-000000000003"),
            availableQuantity,
            new DateTimeOffset(2026, 8, 30, 7, 0, 0, TimeSpan.Zero));

    private sealed record OrderFixture(
        Guid BuyerId,
        Guid InventoryItemId,
        Guid StallId,
        decimal AvailableQuantity,
        DateTimeOffset Now);
}
