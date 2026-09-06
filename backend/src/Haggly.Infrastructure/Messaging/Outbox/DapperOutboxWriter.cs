using System.Text.Json;
using Dapper;
using Haggly.Application.Common.Messaging;
using Haggly.Domain.Common.Events.V1;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Haggly.Infrastructure.Messaging.Outbox;

public sealed class DapperOutboxWriter(
    HagglyDbContext dbContext,
    DomainEventTypeRegistry eventTypes,
    TimeProvider timeProvider) : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string InsertEventSql =
        """
        INSERT INTO messaging.outbox_messages
            ("Id", "EventType", "Payload", "CorrelationId",
             "OccurredAt", "CreatedAt", "ProcessedAt")
        VALUES
            (@Id, @EventType, CAST(@Payload AS jsonb), @CorrelationId,
             @OccurredAt, @CreatedAt, NULL);
        """;

    public async Task WriteAsync<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (domainEvent.EventId == Guid.Empty)
            throw new ArgumentException("A valid event ID is required.", nameof(domainEvent));
        if (domainEvent.CorrelationId == Guid.Empty)
            throw new ArgumentException("A valid correlation ID is required.", nameof(domainEvent));

        var currentTransaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "An active database transaction is required to write an outbox message.");
        var eventType = eventTypes.GetEventType(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, JsonOptions);
        var connection = dbContext.Database.GetDbConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            InsertEventSql,
            new
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Payload = payload,
                domainEvent.CorrelationId,
                domainEvent.OccurredAt,
                CreatedAt = timeProvider.GetUtcNow()
            },
            currentTransaction.GetDbTransaction(),
            cancellationToken: cancellationToken));
    }
}
