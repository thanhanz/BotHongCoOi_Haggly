using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class OrderPaymentFailedConsumerDefinition
    : ConsumerDefinition<OrderPaymentFailedConsumer>
{
    public OrderPaymentFailedConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.OrderPaymentFailedQueue;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderPaymentFailedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15)));
    }
}
