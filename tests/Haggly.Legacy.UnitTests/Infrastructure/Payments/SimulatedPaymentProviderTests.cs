using Haggly.Application.Abstractions.Payments;
using Haggly.Infrastructure.Payments;
using Microsoft.Extensions.Options;
using Xunit;

namespace Haggly.UnitTests.Infrastructure.Payments;

public sealed class SimulatedPaymentProviderTests
{
    [Fact]
    public async Task ProcessAsync_WhenConfiguredForSuccess_ReturnsDeterministicProviderId()
    {
        var transactionId = Guid.NewGuid();
        var provider = CreateProvider(SimulatedPaymentOutcome.Success);

        var result = await provider.ProcessAsync(CreateRequest(transactionId));

        Assert.True(result.Succeeded);
        Assert.Equal($"SIM-{transactionId:N}".ToUpperInvariant(), result.ProviderTransactionId);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task ProcessAsync_WhenConfiguredForFailure_ReturnsConfiguredReason()
    {
        var provider = CreateProvider(SimulatedPaymentOutcome.Failure, "  simulated decline  ");

        var result = await provider.ProcessAsync(CreateRequest(Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Null(result.ProviderTransactionId);
        Assert.Equal("simulated decline", result.FailureReason);
    }

    private static SimulatedPaymentProvider CreateProvider(
        SimulatedPaymentOutcome outcome,
        string failureReason = "Simulated provider decline.")
        => new(Options.Create(new SimulatedPaymentOptions
        {
            Outcome = outcome,
            FailureReason = failureReason
        }));

    private static PaymentProviderRequest CreateRequest(Guid transactionId)
        => new(Guid.NewGuid(), transactionId, 300_000m, "VND");
}
