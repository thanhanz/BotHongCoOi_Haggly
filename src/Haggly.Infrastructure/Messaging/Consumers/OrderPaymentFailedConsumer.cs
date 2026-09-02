using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Sales.Events.V1;
using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class OrderPaymentFailedConsumer(OrderPaymentFailedHandler handler)
    : IConsumer<PaymentFailedEvent>
{
    public Task Consume(ConsumeContext<PaymentFailedEvent> context)
        => handler.HandleAsync(context.Message, context.CancellationToken);
}
