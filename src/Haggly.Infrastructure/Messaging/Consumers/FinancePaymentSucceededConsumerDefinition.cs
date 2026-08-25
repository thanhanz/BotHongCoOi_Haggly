using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class FinancePaymentSucceededConsumerDefinition : ConsumerDefinition<FinancePaymentSucceededConsumer>
{
    public FinancePaymentSucceededConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.FinancePaymentSucceededQueue;
    }
}
