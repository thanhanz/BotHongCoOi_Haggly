using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Infrastructure.Messaging.Faults;
using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class PaymentFailedFaultConsumerDefinition
    : ConsumerDefinition<LoggingFaultConsumer<PaymentFailedEvent>>
{
    public PaymentFailedFaultConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.PaymentProcessingFaultsQueue;
    }
}
