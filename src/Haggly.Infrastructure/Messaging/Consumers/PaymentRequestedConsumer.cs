using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Events.V1;
using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class PaymentRequestedConsumer(ProcessPaymentRequestedHandler handler)
    : IConsumer<PaymentRequested>
{
    public Task Consume(ConsumeContext<PaymentRequested> context)
        => handler.HandleAsync(context.Message, context.CancellationToken);
}
