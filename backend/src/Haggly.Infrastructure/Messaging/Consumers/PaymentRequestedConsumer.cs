using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Events.V1;
using MassTransit;
using Haggly.Infrastructure.Messaging.Outbox;
using Microsoft.Extensions.Options;

namespace Haggly.Infrastructure.Messaging.Consumers;

public sealed class PaymentRequestedConsumer(
    ProcessPaymentRequestedHandler handler,
    IOptions<OutboxBenchmarkOptions> benchmarkOptions)
    : IConsumer<PaymentRequested>
{
    private readonly OutboxBenchmarkOptions benchmarkSettings = benchmarkOptions.Value;

    public Task Consume(ConsumeContext<PaymentRequested> context)
        => benchmarkSettings.Enabled
            ? Task.CompletedTask
            : handler.HandleAsync(context.Message, context.CancellationToken);
}
