using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class OrderPaymentSucceededConsumerDefinition : ConsumerDefinition<OrderPaymentSucceededConsumer>
{
    public OrderPaymentSucceededConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.OrderPaymentSucceededQueue;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderPaymentSucceededConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15)));
    }
}
