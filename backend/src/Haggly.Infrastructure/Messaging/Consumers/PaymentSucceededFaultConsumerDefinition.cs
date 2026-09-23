using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Infrastructure.Messaging.Faults;
using MassTransit;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class PaymentSucceededFaultConsumerDefinition
    : ConsumerDefinition<LoggingFaultConsumer<PaymentSucceededEvent>>
{
    public PaymentSucceededFaultConsumerDefinition()
    {
        EndpointName = PaymentMessagingNames.PaymentProcessingFaultsQueue;
    }
}
