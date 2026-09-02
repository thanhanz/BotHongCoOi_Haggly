using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class InventoryPaymentFailedConsumerDefinition
    : ConsumerDefinition<InventoryPaymentFailedConsumer>
{
    public InventoryPaymentFailedConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.InventoryPaymentFailedQueue;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<InventoryPaymentFailedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15)));
    }
}
