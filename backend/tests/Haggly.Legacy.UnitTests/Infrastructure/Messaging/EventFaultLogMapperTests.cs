using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Infrastructure.Messaging;
using Haggly.Infrastructure.Messaging.Faults;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Messaging;

public sealed class EventFaultLogMapperTests
{
    [Theory]
    [InlineData(PaymentMessagingNames.InventoryPaymentSucceededQueue, "Inventory")]
    [InlineData(PaymentMessagingNames.FinancePaymentSucceededQueue, "Finance")]
    [InlineData(PaymentMessagingNames.OrderPaymentSucceededQueue, "Order")]
    public void Map_KnownPaymentConsumerSource_ReturnsOwningComponent(
        string queueName,
        string expectedComponent)
    {
        var fault = CreateFault($"rabbitmq://localhost/{queueName}");

        var result = EventFaultLogMapper.Map(fault);

        Assert.Equal(expectedComponent, result.Component);
    }

    [Fact]
    public void Map_InvalidConsumerSource_ReturnsUnknownComponent()
    {
        var fault = CreateFault("not-a-source-address");

        var result = EventFaultLogMapper.Map(fault);

        Assert.Equal("Unknown", result.Component);
    }

    [Fact]
    public void Map_FaultWithExceptions_PreservesFailureDetails()
    {
        var fault = CreateFault(
            $"rabbitmq://localhost/{PaymentMessagingNames.InventoryPaymentSucceededQueue}");

        var result = EventFaultLogMapper.Map(fault);

        var exception = Assert.Single(result.Fault.Exceptions);
        Assert.Equal("System.InvalidOperationException", exception.ExceptionType);
        Assert.Equal("The paid order has no active inventory items.", exception.Message);
        Assert.Equal("at InventoryPaymentSucceededHandler.HandleAsync()", exception.StackTrace);
    }

    private static EventFaultMetadata<PaymentSucceededEvent> CreateFault(
        string? sourceAddress)
    {
        var occurredAt = new DateTimeOffset(2026, 8, 25, 1, 2, 3, TimeSpan.Zero);
        var message = new PaymentSucceededEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            occurredAt,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            300_000m,
            "VND",
            "provider-transaction-1",
            [Guid.NewGuid()]);

        return new EventFaultMetadata<PaymentSucceededEvent>(
            Guid.NewGuid(),
            Guid.NewGuid(),
            message.CorrelationId,
            occurredAt.AddSeconds(1),
            sourceAddress,
            "haggly-test-host",
            message,
            [
                new EventFaultException(
                    "System.InvalidOperationException",
                    "The paid order has no active inventory items.",
                    "at InventoryPaymentSucceededHandler.HandleAsync()")
            ]);
    }
}
