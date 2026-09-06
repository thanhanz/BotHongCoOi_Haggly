using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common;
using Haggly.Application.Modules.Sales.Exceptions;
using Haggly.Application.Modules.Sales.Queries;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.Queries;

public sealed class GetCartHandlerTests
{
    private readonly ICartQuery _query = Substitute.For<ICartQuery>();

    [Fact]
    public async Task Handle_MissingCart_ReturnsEmptyBuyerCart()
    {
        // Arrange
        _query.GetAsync(BuyerId, Arg.Any<CancellationToken>()).Returns((CartReadModel?)null);

        // Act
        var result = await new GetCartHandler(_query).Handle(new GetCartQuery(BuyerId), CancellationToken.None);

        // Assert
        Assert.Equal(BuyerId, result.BuyerId);
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Empty(result.Stalls);
    }

    [Fact]
    public async Task Handle_EmptyBuyerId_ThrowsValidationWithoutQuerying()
    {
        // Arrange

        // Act
        var action = () => new GetCartHandler(_query).Handle(new GetCartQuery(Guid.Empty), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CartValidationException>(action);
        await _query.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    private static readonly Guid BuyerId = Guid.Parse("98000000-0000-0000-0000-000000000001");
}

public sealed class GetOrdersHandlerTests
{
    private readonly IOrderQuery _query = Substitute.For<IOrderQuery>();

    [Fact]
    public async Task Handle_ValidPaging_ReturnsMappedPageAndForwardsPaging()
    {
        // Arrange
        var order = new Order { BuyerId = BuyerId, OrderNo = "ORD-1", Status = OrderStatus.NEGOTIATING };
        _query.GetPageAsync(BuyerId, 2, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Order>([order], 2, 10, 11));

        // Act
        var result = await new GetOrdersHandler(_query).Handle(
            new GetOrdersQuery(BuyerId, 2, 10), CancellationToken.None);

        // Assert
        Assert.Equal(order.Id, Assert.Single(result.Items).Id);
        Assert.Equal(11, result.TotalCount);
        await _query.Received(1).GetPageAsync(BuyerId, 2, 10, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(1, 0, 10)]
    [InlineData(1, 1, 101)]
    public async Task Handle_InvalidRequest_ThrowsValidationWithoutQuerying(int buyerValue, int page, int pageSize)
    {
        // Arrange
        var buyerId = buyerValue == 0 ? Guid.Empty : BuyerId;

        // Act
        var action = () => new GetOrdersHandler(_query).Handle(
            new GetOrdersQuery(buyerId, page, pageSize), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderValidationException>(action);
        await _query.DidNotReceive().GetPageAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private static readonly Guid BuyerId = Guid.Parse("98100000-0000-0000-0000-000000000001");
}

public sealed class GetOrderDetailsHandlerTests
{
    private readonly IOrderQuery _query = Substitute.For<IOrderQuery>();

    [Fact]
    public async Task Handle_OwnedOrder_ReturnsMappedOrder()
    {
        // Arrange
        var order = new Order { BuyerId = BuyerId, OrderNo = "ORD-1", Status = OrderStatus.NEGOTIATING };
        _query.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        // Act
        var result = await new GetOrderDetailsHandler(_query).Handle(
            new GetOrderDetailsQuery(order.Id, BuyerId), CancellationToken.None);

        // Assert
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(BuyerId, result.BuyerId);
    }

    [Fact]
    public async Task Handle_OrderBelongsToAnotherBuyer_ThrowsForbidden()
    {
        // Arrange
        var order = new Order { BuyerId = OtherBuyerId };
        _query.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        // Act
        var action = () => new GetOrderDetailsHandler(_query).Handle(
            new GetOrderDetailsQuery(order.Id, BuyerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderForbiddenException>(action);
    }

    [Fact]
    public async Task Handle_MissingOrder_ThrowsNotFound()
    {
        // Arrange
        _query.GetByIdAsync(OrderId, Arg.Any<CancellationToken>()).Returns((Order?)null);

        // Act
        var action = () => new GetOrderDetailsHandler(_query).Handle(
            new GetOrderDetailsQuery(OrderId, BuyerId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<OrderNotFoundException>(action);
    }

    private static readonly Guid OrderId = Guid.Parse("98200000-0000-0000-0000-000000000001");
    private static readonly Guid BuyerId = Guid.Parse("98200000-0000-0000-0000-000000000002");
    private static readonly Guid OtherBuyerId = Guid.Parse("98200000-0000-0000-0000-000000000003");
}
