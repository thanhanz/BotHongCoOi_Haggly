using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class InventoryPaymentSucceededConsumerDefinition : ConsumerDefinition<InventoryPaymentSucceededConsumer>
{
    public InventoryPaymentSucceededConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.InventoryPaymentSucceededQueue;
    }
}
