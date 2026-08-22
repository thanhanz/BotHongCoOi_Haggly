using Haggly.Domain.Common.Events.V1;

namespace Haggly.Application.Modules.Payments.Events.V1;

public sealed record PaymentSucceeded(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid PaymentId,
    Guid PaymentTransactionId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string ProviderTransactionId) : IDomainEvent;
