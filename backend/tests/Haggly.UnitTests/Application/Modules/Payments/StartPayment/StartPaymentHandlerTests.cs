using Haggly.Application.Abstractions.Inventory;
using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Abstractions.Sales;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Commands;
using Haggly.Application.Modules.Payments.Dtos;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Payments.Exceptions;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Payments.StartPayment;

public sealed class StartPaymentHandlerTests
{
    private readonly IPaymentCommandRepository _repository =
        Substitute.For<IPaymentCommandRepository>();

    private readonly IOrderCommandRepository _orderRepository =
        Substitute.For<IOrderCommandRepository>();

    private readonly IInventoryPaymentRepository _inventoryRepository =
        Substitute.For<IInventoryPaymentRepository>();

    private readonly IOutboxWriter _outboxWriter =
        Substitute.For<IOutboxWriter>();

    private readonly IPaymentUnitOfWork _unitOfWork =
        Substitute.For<IPaymentUnitOfWork>();

    private readonly IBusinessClock _clock =
        Substitute.For<IBusinessClock>();

    [Fact]
    public async Task Handle_AgreedOrder_CreatesPaymentAndWritesRequestedEvent()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);
        Payment? createdPayment = null;
        _repository.AddAsync(
                Arg.Do<Payment>(payment => createdPayment = payment),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var command = new StartPaymentCommand(fixture.Order.Id, fixture.BuyerId);

        // Act
        var result = await CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(createdPayment);
        Assert.Equal(PaymentStatus.PENDING, result.Status);
        Assert.Equal(fixture.Order.TotalToCharge, result.AmountDue);
        Assert.Equal(OrderStatus.PAYMENT_PENDING, fixture.Order.Status);
        await _inventoryRepository.Received(1).ReserveAsync(
            fixture.Order.Id,
            fixture.Now,
            Arg.Any<CancellationToken>());
        await _orderRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _outboxWriter.Received(1).WriteAsync(
            Arg.Is<PaymentRequested>(message =>
                message.PaymentId == createdPayment!.Id
                && message.OrderId == fixture.Order.Id
                && message.Amount == fixture.Order.TotalToCharge
                && message.Currency == "VND"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var fixture = CreateFixture();
        _orderRepository.FindForPaymentAsync(
                fixture.Order.Id,
                Arg.Any<CancellationToken>())
            .Returns((Order?)null);
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().Handle(
            new StartPaymentCommand(fixture.Order.Id, fixture.BuyerId),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PaymentNotFoundException>(action);
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<PaymentRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderBelongsToAnotherBuyer_ThrowsForbiddenException()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureOrder(fixture.Order);
        ConfigureTransaction();
        var command = new StartPaymentCommand(
            fixture.Order.Id,
            Guid.Parse("80000000-0000-0000-0000-000000000003"));

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PaymentForbiddenException>(action);
        await _inventoryRepository.DidNotReceive().ReserveAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderIsNotReadyForPayment_ThrowsConflictException()
    {
        // Arrange
        var fixture = CreateFixture();
        fixture.Order.Status = OrderStatus.NEGOTIATING;
        ConfigureOrder(fixture.Order);
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().Handle(
            new StartPaymentCommand(fixture.Order.Id, fixture.BuyerId),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PaymentConflictException>(action);
        await _inventoryRepository.DidNotReceive().ReserveAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaymentAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureOrder(fixture.Order);
        _repository.FindByOrderIdAsync(fixture.Order.Id, Arg.Any<CancellationToken>())
            .Returns(Payment.Create(
                Guid.Parse("80000000-0000-0000-0000-000000000006"),
                fixture.Order.Id,
                fixture.Order.TotalToCharge,
                "VND",
                fixture.Now));
        ConfigureTransaction();

        // Act
        var action = () => CreateSubject().Handle(
            new StartPaymentCommand(fixture.Order.Id, fixture.BuyerId),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PaymentConflictException>(action);
        await _inventoryRepository.DidNotReceive().ReserveAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<PaymentRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LocalClock_WritesUtcRequestedEventMetadata()
    {
        // Arrange
        var fixture = CreateFixture();
        var localTime = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.FromHours(7));
        ConfigureFixture(fixture);
        _clock.GetNow().Returns(localTime);

        // Act
        await CreateSubject().Handle(
            new StartPaymentCommand(fixture.Order.Id, fixture.BuyerId),
            CancellationToken.None);

        // Assert
        await _outboxWriter.Received(1).WriteAsync(
            Arg.Is<PaymentRequested>(message =>
                message.OccurredAt.Offset == TimeSpan.Zero
                && message.OccurredAt.UtcDateTime == localTime.UtcDateTime),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "80000000-0000-0000-0000-000000000002")]
    [InlineData("80000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000000")]
    public async Task Handle_InvalidIdentifiers_ThrowsValidationException(
        string orderId,
        string buyerId)
    {
        // Arrange
        var command = new StartPaymentCommand(Guid.Parse(orderId), Guid.Parse(buyerId));

        // Act
        var action = () => CreateSubject().Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PaymentValidationException>(action);
        await _unitOfWork.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<PaymentDto>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InventoryReservationFails_PropagatesFailureWithoutPayment()
    {
        // Arrange
        var fixture = CreateFixture();
        ConfigureFixture(fixture);
        _inventoryRepository.ReserveAsync(
                fixture.Order.Id,
                fixture.Now,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("insufficient stock")));

        // Act
        var action = () => CreateSubject().Handle(
            new StartPaymentCommand(fixture.Order.Id, fixture.BuyerId),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _outboxWriter.DidNotReceive().WriteAsync(
            Arg.Any<PaymentRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var fixture = CreateFixture();
        var cancellationToken = new CancellationToken(canceled: true);
        _orderRepository.FindForPaymentAsync(fixture.Order.Id, cancellationToken)
            .Returns(Task.FromCanceled<Order?>(cancellationToken));
        ConfigureTransaction();
        var command = new StartPaymentCommand(fixture.Order.Id, fixture.BuyerId);

        // Act
        var action = () => CreateSubject().Handle(command, cancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        await _orderRepository.Received(1).FindForPaymentAsync(
            fixture.Order.Id, cancellationToken);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private StartPaymentHandler CreateSubject()
        => new(
            _repository,
            _orderRepository,
            _inventoryRepository,
            _outboxWriter,
            _unitOfWork,
            _clock);

    private void ConfigureFixture(PaymentFixture fixture)
    {
        ConfigureOrder(fixture.Order);
        _repository.FindByOrderIdAsync(
                fixture.Order.Id,
                Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        _inventoryRepository.ReserveAsync(
                fixture.Order.Id,
                fixture.Now,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _clock.GetNow().Returns(fixture.Now);
        ConfigureTransaction();
    }

    private void ConfigureOrder(Order order)
        => _orderRepository.FindForPaymentAsync(
                order.Id,
                Arg.Any<CancellationToken>())
            .Returns(order);

    private void ConfigureTransaction()
        => _unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<PaymentDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<PaymentDto>>>();
                var cancellationToken = callInfo.ArgAt<CancellationToken>(1);
                return operation(cancellationToken);
            });

    private static PaymentFixture CreateFixture()
    {
        var buyerId = Guid.Parse("80000000-0000-0000-0000-000000000002");
        var now = new DateTimeOffset(2026, 8, 30, 5, 0, 0, TimeSpan.Zero);
        var order = Order.Place(
            Guid.Parse("80000000-0000-0000-0000-000000000001"),
            buyerId,
            [new OrderItemInput(
                Guid.Parse("80000000-0000-0000-0000-000000000004"),
                Guid.Parse("80000000-0000-0000-0000-000000000005"),
                "Rice", ProductUnit.KG, 30_000m, 10m, null)],
            now);
        order.Status = OrderStatus.AGREED;
        return new PaymentFixture(buyerId, order, now);
    }

    private sealed record PaymentFixture(
        Guid BuyerId,
        Order Order,
        DateTimeOffset Now);
}
