using Dapper;
using Haggly.Application.Common.Messaging;
using Haggly.Domain.Common.Events.V1;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Haggly.Infrastructure.Messaging.Outbox;

public sealed class DapperOutboxProcessor(
    HagglyDbContext dbContext,
    DomainEventTypeRegistry eventTypes,
    IDomainEventPublisher publisher,
    TimeProvider timeProvider) : IOutboxProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string QueryPendingEventsSql =
        """
        SELECT "Id", "EventType", "Payload"::text AS "Payload"
        FROM messaging.outbox_messages
        WHERE "ProcessedAt" IS NULL
        ORDER BY "CreatedAt", "Id"
        LIMIT @BatchSize;
        """;


    private const string UpdateProcessedSql =
        """
        UPDATE messaging.outbox_messages
        SET "ProcessedAt" = @ProcessedAt,
            "ErrorMessage" = NULL
        WHERE "Id" = @Id;
        """;

    private const string UpdateErrorSql =
        """
        UPDATE messaging.outbox_messages
        SET "ErrorMessage" = @ErrorMessage
        WHERE "Id" = @Id;
        """;

    //TODO: This function need to optimize about the performance
    public async Task<int> ProcessPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
      
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var connection = dbContext.Database.GetDbConnection();
        
        //Query un_processed (processed_at is null) events
        var messages = await connection.QueryAsync<PendingOutboxMessage>(new CommandDefinition(
          QueryPendingEventsSql,
            new { BatchSize = batchSize },
            cancellationToken: cancellationToken));
        
        var processedCount = 0;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var clrType = eventTypes.GetClrType(message.EventType);
                var domainEvent = JsonSerializer.Deserialize(message.Payload, clrType, JsonOptions) as IDomainEvent
                    ?? throw new JsonException(
                        $"Payload for '{message.EventType}' could not be deserialized as a domain event.");

                await publisher.PublishAsync(domainEvent, cancellationToken);
                
                await connection.ExecuteAsync(new CommandDefinition(
                  UpdateProcessedSql,
                    new { message.Id, ProcessedAt = timeProvider.GetUtcNow() },
                    cancellationToken: cancellationToken));
                processedCount++;
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                var errorMessage = exception.Message.Length <= 2000
                    ? exception.Message
                    : exception.Message[..2000];
                await connection.ExecuteAsync(new CommandDefinition(
                    UpdateErrorSql,
                    new { message.Id, ErrorMessage = errorMessage },
                    cancellationToken: cancellationToken));
            }
        }

        return processedCount;
    }

    private sealed record PendingOutboxMessage(Guid Id, string EventType, string Payload);
}
