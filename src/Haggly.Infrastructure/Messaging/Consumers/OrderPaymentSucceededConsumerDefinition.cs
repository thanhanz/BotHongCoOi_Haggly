using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class OrderPaymentSucceededConsumerDefinition : ConsumerDefinition<OrderPaymentSucceededConsumer>
{
    public OrderPaymentSucceededConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.OrderPaymentSucceededQueue;
    }
}
