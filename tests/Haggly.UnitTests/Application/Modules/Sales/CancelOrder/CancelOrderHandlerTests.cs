using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Sales.Commands;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;
using DomainOrder = Haggly.Domain.Modules.Sales.Order;

namespace Haggly.UnitTests.Application.Modules.Sales.CancelOrder;

public sealed class CancelOrderHandlerTests
{
    private readonly IOrderCommandRepository _repository = Substitute.For<IOrderCommandRepository>();

    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_OwnedNegotiatingOrder_CancelsAndSavesOrder()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureOrder(fixture.Order);
        var command = new CancelOrderCommand(fixture.Order.Id, fixture.BuyerId, "Changed my mind");

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(OrderStatus.CANCELLED, result.Status);
        Assert.Equal("Changed my mind", result.CancellationReason);
        Assert.Equal(OrderStatus.CANCELLED, fixture.Order.Status);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var fixture = CreateFixture();
        _repository.FindByIdAsync(fixture.Order.Id, Arg.Any<CancellationToken>())
            .Returns((DomainOrder?)null);
        var command = new CancelOrderCommand(fixture.Order.Id, fixture.BuyerId, "Changed my mind");

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderNotFoundException>(action);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderBelongsToAnotherBuyer_ThrowsForbiddenException()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureOrder(fixture.Order);
        var otherBuyerId = Guid.Parse("B0000000-0000-0000-0000-000000000003");
        var command = new CancelOrderCommand(fixture.Order.Id, otherBuyerId, "Changed my mind");

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderForbiddenException>(action);
        Assert.Equal(OrderStatus.NEGOTIATING, fixture.Order.Status);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidReason_ThrowsValidationException(string reason)
    {
        // Arrange
        var fixture = CreateFixture();
        var command = new CancelOrderCommand(fixture.Order.Id, fixture.BuyerId, reason);

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderValidationException>(action);
        await _repository.DidNotReceive().FindByIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaidOrder_TranslatesDomainFailureWithoutSave()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Order.Status = OrderStatus.PAID;
        ConfigureOrder(fixture.Order);
        var command = new CancelOrderCommand(fixture.Order.Id, fixture.BuyerId, "Too late");

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderConflictException>(action);
        Assert.Equal(OrderStatus.PAID, fixture.Order.Status);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_ForwardsTokenToRepository()
    {
        // Arrange
        var fixture = CreateFixture();
        var cancellationToken = new CancellationToken(canceled: true);
        _repository.FindByIdAsync(fixture.Order.Id, cancellationToken)
            .Returns(Task.FromCanceled<DomainOrder?>(cancellationToken));
        var command = new CancelOrderCommand(fixture.Order.Id, fixture.BuyerId, "Changed my mind");

        // Act
        var action = () => CreateSubject().Handle(command, cancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _repository.Received(1).FindByIdAsync(fixture.Order.Id, cancellationToken);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private CancelOrderHandler CreateSubject()
        => new(_repository, _clock);

    private void ConfigureOrder(DomainOrder order)
    {
        _repository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);
        _clock.GetNow().Returns(Now);
    }

    private static CancelFixture CreateFixture()
    {
        var buyerId = Guid.Parse("B0000000-0000-0000-0000-000000000001");
        var order = DomainOrder.Place(
            Guid.Parse("B0000000-0000-0000-0000-000000000002"),
            buyerId,
            [new OrderItemInput(
                Guid.Parse("B0000000-0000-0000-0000-000000000004"),
                Guid.Parse("B0000000-0000-0000-0000-000000000005"),
                "Tomato", ProductUnit.KG, 45_000m, 1m, null)],
            Now);
        return new CancelFixture(buyerId, order);
    }

    private static readonly DateTimeOffset Now =
        new(2026, 8, 30, 8, 0, 0, TimeSpan.Zero);

    private sealed record CancelFixture(Guid BuyerId, DomainOrder Order);
}
