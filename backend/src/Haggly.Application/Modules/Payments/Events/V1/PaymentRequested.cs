using Haggly.Domain.Common.Events.V1;

namespace Haggly.Application.Modules.Payments.Events.V1;

public sealed record PaymentRequested(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAt,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency) : IDomainEvent;
