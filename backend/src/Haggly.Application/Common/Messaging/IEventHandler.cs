using Haggly.Domain.Common.Events.V1;

namespace Haggly.Application.Common.Messaging;

public interface IEventHandler<in TEvent> where TEvent : class, IDomainEvent
{
    Task HandleAsync(TEvent message, CancellationToken cancellationToken);
}
