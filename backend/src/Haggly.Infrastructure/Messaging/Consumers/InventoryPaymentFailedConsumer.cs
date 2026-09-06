using Haggly.Application.Modules.Inventory.Events.V1;
using Haggly.Application.Modules.Payments.Events.V1;
using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class InventoryPaymentFailedConsumer(InventoryPaymentFailedHandler handler)
    : IConsumer<PaymentFailedEvent>
{
    public Task Consume(ConsumeContext<PaymentFailedEvent> context)
        => handler.HandleAsync(context.Message, context.CancellationToken);
}
