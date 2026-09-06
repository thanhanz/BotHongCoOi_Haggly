using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Sales.Events.V1;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Sales.PaymentResults;

public sealed class OrderPaymentFailedHandlerTests
{
    private readonly IOrderCommandRepository _orders = Substitute.For<IOrderCommandRepository>();
    private readonly IInboxRepository _inbox = Substitute.For<IInboxRepository>();
    private readonly ISalesTransactionExecutor _transaction = Substitute.For<ISalesTransactionExecutor>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();

    [Fact]
    public async Task HandleAsync_NewFailureForPendingPayment_MovesOrderBackToAgreed()
    {
        // Arrange
        var order = CreateOrder(OrderStatus.PAYMENT_PENDING);
        var message = CreateMessage(order);
        _inbox.TryAddAsync(Arg.Any<string>(), message.EventId, Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(true);
        _orders.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _clock.GetNow().Returns(Now);
        ConfigureTransaction();

        // Act
        await CreateSubject().HandleAsync(message, CancellationToken.None);

        // Assert
        Assert.Equal(OrderStatus.AGREED, order.Status);
        await _orders.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateFailure_DoesNotLoadOrSaveOrder()
    {
        // Arrange
        var order = CreateOrder(OrderStatus.PAYMENT_PENDING);
        var message = CreateMessage(order);
        _inbox.TryAddAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);
        ConfigureTransaction();

        // Act
        await CreateSubject().HandleAsync(message, CancellationToken.None);

        // Assert
        await _orders.DidNotReceive().FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _orders.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private OrderPaymentFailedHandler CreateSubject() => new(_orders, _inbox, _transaction, _clock);
    private void ConfigureTransaction() => _transaction.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Func<CancellationToken, Task<bool>>>()(ci.ArgAt<CancellationToken>(1)));
    private static Order CreateOrder(OrderStatus status) { var order = Order.Place(Guid.Parse("F5000000-0000-0000-0000-000000000001"), Guid.Parse("F5000000-0000-0000-0000-000000000002"), [new OrderItemInput(Guid.Parse("F5000000-0000-0000-0000-000000000003"), Guid.Parse("F5000000-0000-0000-0000-000000000004"), "Rice", ProductUnit.KG, 50_000m, 2m, null)], Now.AddMinutes(-1)); order.Status = status; return order; }
    private static PaymentFailedEvent CreateMessage(Order order) => new(Guid.Parse("F5000000-0000-0000-0000-000000000005"), Guid.Parse("F5000000-0000-0000-0000-000000000006"), Now, Guid.Parse("F5000000-0000-0000-0000-000000000007"), Guid.Parse("F5000000-0000-0000-0000-000000000008"), order.Id, order.TotalToCharge, order.Currency, "declined");
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 17, 0, 0, TimeSpan.Zero);
}
