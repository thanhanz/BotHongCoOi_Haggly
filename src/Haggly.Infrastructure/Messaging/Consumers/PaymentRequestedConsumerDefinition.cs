using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class PaymentRequestedConsumerDefinition: ConsumerDefinition<PaymentRequestedConsumer>
{
    public PaymentRequestedConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.PaymentRequestedQueue;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PaymentRequestedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Immediate retry on transient failure
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15)));
    }
}
