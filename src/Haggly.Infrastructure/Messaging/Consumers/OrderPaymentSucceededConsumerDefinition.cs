using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class OrderPaymentSucceededConsumerDefinition : ConsumerDefinition<OrderPaymentSucceededConsumer>
{
    public OrderPaymentSucceededConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.OrderPaymentSucceededQueue;
    }
    
    // Prevent "order-payment-succeeded-v1_error" queue created (Automatically)
    // Be handle by other queue (Discarded) when Masstrasit raise Fault<TEvent>
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderPaymentSucceededConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.DiscardFaultedMessages();
    }
}
