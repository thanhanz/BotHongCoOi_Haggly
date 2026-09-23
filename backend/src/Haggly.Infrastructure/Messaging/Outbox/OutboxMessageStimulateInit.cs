using Dapper;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Haggly.Infrastructure.Messaging.Outbox;

public sealed class OutboxMessageStimulateInit(
    HagglyDbContext dbContext,
    // DomainEventTypeRegistry eventTypes,
    TimeProvider timeProvider,
    ILogger<OutboxMessageStimulateInit> logger)
{
    private const int TotalEventCount = 1_000_000;
    private const int InsertBatchSize = 200_000;
    private const decimal SimulatedAmount = 100_000m;
    private const string SimulatedCurrency = "VND";

    private const string TruncateSql =
        "TRUNCATE TABLE messaging.outbox_messages;";

    private const string InsertBatchSql =
        """
        WITH generated_events AS MATERIALIZED
        (
            SELECT
                gen_random_uuid() AS event_id,
                gen_random_uuid() AS correlation_id,
                gen_random_uuid() AS payment_id,
                gen_random_uuid() AS order_id
            FROM generate_series(1, @BatchSize)
        )
        INSERT INTO messaging.outbox_messages
            ("Id", "EventType", "Payload", "CorrelationId",
             "OccurredAt", "ProcessedAt", "ErrorMessage")
        SELECT
            gen_random_uuid(),
            @EventType,
            jsonb_build_object(
                'eventId', event_id,
                'correlationId', correlation_id,
                'occurredAt', @OccurredAt,
                'paymentId', payment_id,
                'orderId', order_id,
                'amount', @Amount,
                'currency', @Currency),
            correlation_id,
            @OccurredAt,
            NULL,
            NULL
        FROM generated_events;
        """;

    public async Task SeedDataAsync(CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        var eventType = nameof(PaymentRequested);

        await connection.ExecuteAsync(new CommandDefinition(
            TruncateSql,
            cancellationToken: cancellationToken,
            commandTimeout: 0));

        var totalInserted = 0;
        while (totalInserted < TotalEventCount)
        {
            var batchSize = Math.Min(InsertBatchSize, TotalEventCount - totalInserted);
            var now = timeProvider.GetUtcNow();
            var insertedCount = await connection.ExecuteAsync(new CommandDefinition(
                InsertBatchSql,
                new
                {
                    BatchSize = batchSize,
                    EventType = eventType,
                    OccurredAt = now,
                    Amount = SimulatedAmount,
                    Currency = SimulatedCurrency
                },
                cancellationToken: cancellationToken,
                commandTimeout: 0));

            totalInserted += insertedCount;
            logger.LogInformation(
                "Inserted {InsertedCount} simulated PaymentRequested events. Total inserted: {TotalInserted}/{TotalEventCount}.",
                insertedCount,
                totalInserted,
                TotalEventCount);
        }

        logger.LogInformation(
            "Finished inserting {TotalInserted} simulated PaymentRequested events into the outbox message table.",
            totalInserted);
    }
}
