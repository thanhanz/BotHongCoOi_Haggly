using Dapper;
using Haggly.Infrastructure.Messaging.Inbox;
using Haggly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Inbox;

public sealed class DapperInboxRepositoryTests
{
    [Fact]
    public async Task TryAddAsync_WhenMessageDoesNotExist_ReturnsTrueAndPersistsMessage()
    {
        var eventId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var repository = new DapperInboxRepository(dbContext);

        var added = await repository.TryAddAsync(
            "inventory-payment-failed-v1",
            eventId,
            "payments.payment-failed.v1",
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);

        await using var connection = await new DapperDbContext(
            IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None);
        var storedCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM messaging.inbox_messages
            WHERE "ConsumerName" = @ConsumerName AND "EventId" = @EventId;
            """,
            new { ConsumerName = "inventory-payment-failed-v1", EventId = eventId });

        Assert.True(added);
        Assert.Equal(1, storedCount);
    }

    [Fact]
    public async Task TryAddAsync_WhenMessageExists_ReturnsFalseAndDoesNotAddAnotherMessage()
    {
        var eventId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var repository = new DapperInboxRepository(dbContext);

        var firstAdded = await repository.TryAddAsync(
            "order-payment-failed-v1",
            eventId,
            "payments.payment-failed.v1",
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        var duplicateAdded = await repository.TryAddAsync(
            "order-payment-failed-v1",
            eventId,
            "payments.payment-failed.v1",
            DateTimeOffset.UtcNow.AddSeconds(1),
            CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);

        await using var connection = await new DapperDbContext(
            IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None);
        var storedCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM messaging.inbox_messages
            WHERE "ConsumerName" = @ConsumerName AND "EventId" = @EventId;
            """,
            new { ConsumerName = "order-payment-failed-v1", EventId = eventId });

        Assert.True(firstAdded);
        Assert.False(duplicateAdded);
        Assert.Equal(1, storedCount);
    }

    private static HagglyDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql(IntegrationTestDatabase.ConnectionString)
            .Options);
}
