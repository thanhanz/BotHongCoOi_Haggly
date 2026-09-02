using Haggly.Domain.Common.Events.V1;

namespace Haggly.Application.Common.Messaging;

public interface IOutboxWriter
{
    Task WriteAsync<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent;
}
