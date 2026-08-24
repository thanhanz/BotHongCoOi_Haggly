using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class InventoryPaymentSucceededConsumerDefinition : ConsumerDefinition<InventoryPaymentSucceededConsumer>
{
    public InventoryPaymentSucceededConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.InventoryPaymentSucceededQueue;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<InventoryPaymentSucceededConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15)));
    }
}
