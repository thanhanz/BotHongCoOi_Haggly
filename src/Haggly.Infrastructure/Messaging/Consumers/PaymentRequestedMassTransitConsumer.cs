using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Events.V1;
using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class PaymentRequestedMassTransitConsumer(
    IDomainEventConsumer<PaymentRequested> consumer)
    : IConsumer<PaymentRequested>
{
    public Task Consume(ConsumeContext<PaymentRequested> context)
        => consumer.ConsumeAsync(context.Message, context.CancellationToken);
}
