namespace Haggly.Domain.Common.Events.V1;

/// <summary>
/// Identifies an immutable V1 event contract published across module boundaries.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    Guid CorrelationId { get; }
    DateTimeOffset OccurredAt { get; }
}
