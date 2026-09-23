namespace Haggly.Application.Abstractions.Payments;

public interface IPaymentProvider
{
    Task<PaymentProviderResult> ProcessAsync(
        PaymentProviderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentProviderRequest(
    Guid PaymentId,
    Guid PaymentTransactionId,
    decimal Amount,
    string Currency);

public sealed record PaymentProviderResult(
    bool Succeeded,
    string? ProviderTransactionId,
    string? FailureReason);
