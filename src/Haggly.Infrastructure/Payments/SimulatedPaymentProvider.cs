using Haggly.Application.Abstractions.Payments;
using Microsoft.Extensions.Options;

namespace Haggly.Infrastructure.Payments;

public sealed class SimulatedPaymentProvider(IOptions<SimulatedPaymentOptions> options)
    : IPaymentProvider
{
    private readonly SimulatedPaymentOptions _options = options.Value;

    public Task<PaymentProviderResult> ProcessAsync(
        PaymentProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _options.Outcome switch
        {
            SimulatedPaymentOutcome.Success => new PaymentProviderResult(
                true,
                $"SIM-{request.PaymentTransactionId:N}".ToUpperInvariant(),
                null),
            SimulatedPaymentOutcome.Failure => new PaymentProviderResult(
                false,
                null,
                _options.FailureReason.Trim()),
            _ => throw new InvalidOperationException("Unsupported simulated payment outcome.")
        };

        return Task.FromResult(result);
    }
}
