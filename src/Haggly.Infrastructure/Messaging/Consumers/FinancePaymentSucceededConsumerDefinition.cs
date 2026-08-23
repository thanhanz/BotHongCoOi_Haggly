using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class FinancePaymentSucceededConsumerDefinition
    : ConsumerDefinition<FinancePaymentSucceededConsumer>
{
    public FinancePaymentSucceededConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.FinancePaymentSucceededQueue;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<FinancePaymentSucceededConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15)));
    }
}
