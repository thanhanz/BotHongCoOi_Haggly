using Dapper;
using Haggly.Application.Common.Messaging;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Haggly.Infrastructure.Messaging.Inbox;

public sealed class DapperInboxRepository(HagglyDbContext dbContext) : IInboxRepository
{
    private const string TryAddSql =
        """
        INSERT INTO messaging.inbox_messages
            ("ConsumerName", "EventId", "EventType", "ProcessedAt")
        VALUES
            (@ConsumerName, @EventId, @EventType, @ProcessedAt)
        ON CONFLICT ("ConsumerName", "EventId") DO NOTHING
        RETURNING "EventId";
        """;

    public async Task<bool> TryAddAsync(
        string consumerName,
        Guid eventId,
        string eventType,
        DateTimeOffset processedAt,
        CancellationToken cancellationToken)
    {
        var currentTransaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "An active database transaction is required to add an inbox message.");
        var connection = dbContext.Database.GetDbConnection();
        var addedEventId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            TryAddSql,
            new
            {
                ConsumerName = consumerName,
                EventId = eventId,
                EventType = eventType,
                ProcessedAt = processedAt
            },
            currentTransaction.GetDbTransaction(),
            cancellationToken: cancellationToken));

        return addedEventId.HasValue;
    }
}
