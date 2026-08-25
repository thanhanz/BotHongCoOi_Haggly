using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class FinancePaymentSucceededConsumerDefinition : ConsumerDefinition<FinancePaymentSucceededConsumer>
{
    public FinancePaymentSucceededConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.FinancePaymentSucceededQueue;
    }

    // Prevent "finance-payment-succeeded-v1_error" queue created (Automatically)
    // Be handle by other queue (Discarded) when Masstrasit raise Fault<TEvent>

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<FinancePaymentSucceededConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.DiscardFaultedMessages();
    }
}
