using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class InventoryPaymentSucceededConsumerDefinition : ConsumerDefinition<InventoryPaymentSucceededConsumer>
{
    public InventoryPaymentSucceededConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.InventoryPaymentSucceededQueue;
    }

    // Prevent "inventory-payment-succeeded-v1_error" queue created (Automatically)
    // Be handle by other queue (Discarded) when Masstrasit raise Fault<TEvent>

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<InventoryPaymentSucceededConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.DiscardFaultedMessages();
    }
}
