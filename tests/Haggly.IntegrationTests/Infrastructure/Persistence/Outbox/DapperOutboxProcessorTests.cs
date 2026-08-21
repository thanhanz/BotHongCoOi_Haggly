using Dapper;
using Haggly.Domain.Common.Events.V1;
using Haggly.Infrastructure.Messaging.Outbox;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Outbox;

public sealed class DapperOutboxProcessorTests
{
    [Fact]
    public async Task WriteAsync_WhenTransactionCommits_PersistsOutboxMessage()
    {
        var domainEvent = CreateEvent();
        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var processor = CreateProcessor(dbContext);

        await processor.WriteAsync(domainEvent, CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);

        await using var connection = await new DapperDbContext(
            IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None);
        var stored = await connection.QuerySingleAsync<StoredOutboxMessage>(
            """
            SELECT "Id", "EventType", "Payload"::text AS "Payload",
                   "CorrelationId", "OccurredAt", "CreatedAt", "ProcessedAt"
            FROM messaging.outbox_messages
            WHERE "CorrelationId" = @CorrelationId;
            """,
            new { domainEvent.CorrelationId });

        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.Equal("tests.outbox-event.v1", stored.EventType);
        Assert.Equal(domainEvent.CorrelationId, stored.CorrelationId);
        Assert.True(
            (domainEvent.OccurredAt.UtcDateTime - stored.OccurredAt.ToUniversalTime()).Duration()
            < TimeSpan.FromMicroseconds(1));
        Assert.NotEqual(default, stored.CreatedAt);
        Assert.Null(stored.ProcessedAt);
        Assert.Contains(domainEvent.Value, stored.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteAsync_WhenTransactionRollsBack_DoesNotPersistOutboxMessage()
    {
        var domainEvent = CreateEvent();
        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var processor = CreateProcessor(dbContext);

        await processor.WriteAsync(domainEvent, CancellationToken.None);
        await transaction.RollbackAsync(CancellationToken.None);

        await using var connection = await new DapperDbContext(
            IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None);
        var count = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM messaging.outbox_messages
            WHERE "CorrelationId" = @CorrelationId;
            """,
            new { domainEvent.CorrelationId });

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task WriteAsync_WithoutActiveTransaction_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();
        var processor = CreateProcessor(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.WriteAsync(CreateEvent(), CancellationToken.None));

        Assert.Contains("active database transaction", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DapperOutboxProcessor CreateProcessor(HagglyDbContext dbContext)
        => new(
            dbContext,
            new DomainEventTypeRegistry(
            [
                DomainEventTypeRegistration.For<TestOutboxEvent>("tests.outbox-event.v1")
            ]),
            TimeProvider.System);

    private static HagglyDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql(IntegrationTestDatabase.ConnectionString)
            .Options);

    private static TestOutboxEvent CreateEvent()
        => new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, "outbox-payload");

    private sealed record TestOutboxEvent(
        Guid EventId,
        Guid CorrelationId,
        DateTimeOffset OccurredAt,
        string Value) : IDomainEvent;

    private sealed record StoredOutboxMessage(
        Guid Id,
        string EventType,
        string Payload,
        Guid CorrelationId,
        DateTime OccurredAt,
        DateTime CreatedAt,
        DateTime? ProcessedAt);
}
