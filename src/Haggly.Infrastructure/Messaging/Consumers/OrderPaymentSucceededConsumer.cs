using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Sales.Events.V1;
using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class OrderPaymentSucceededConsumer(OrderPaymentSucceededHandler handler) : IConsumer<PaymentSucceededEvent>
{
    public Task Consume(ConsumeContext<PaymentSucceededEvent> context)
        => handler.HandleAsync(context.Message, context.CancellationToken);
}
