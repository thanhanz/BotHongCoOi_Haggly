using Dapper;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Commands;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Common.Events.V1;
using Haggly.Infrastructure.Messaging.Outbox;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Repositories.Payments;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Haggly.IntegrationTests.Infrastructure.Persistence.Payments;

public sealed class StartPaymentAtomicityTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 21, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WhenPaymentAndOutboxSucceed_CommitsBothRecords()
    {
        var orderId = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext, CreateWriter(dbContext));

        var result = await handler.Handle(new StartPaymentCommand(orderId), CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM payments.payments WHERE \"Id\" = @PaymentId;",
            new { PaymentId = result.Id }));
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM messaging.outbox_messages WHERE \"CorrelationId\" = @PaymentId;",
            new { PaymentId = result.Id }));
    }

    [Fact]
    public async Task Handle_WhenOutboxWriteFails_RollsBackPaymentRecord()
    {
        var orderId = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext, new FailingOutboxWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new StartPaymentCommand(orderId), CancellationToken.None));

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM payments.payments WHERE \"OrderId\" = @OrderId;",
            new { OrderId = orderId }));
    }

    private static StartPaymentHandler CreateHandler(
        HagglyDbContext dbContext,
        IOutboxWriter outboxWriter)
        => new(
            new EfPaymentCommandRepository(dbContext),
            outboxWriter,
            new EfPaymentUnitOfWork(dbContext),
            new FixedBusinessClock(Now));

    private static DapperOutboxWriter CreateWriter(HagglyDbContext dbContext)
        => new(
            dbContext,
            new DomainEventTypeRegistry(
            [
                DomainEventTypeRegistration.For<PaymentRequested>("payments.payment-requested.v1")
            ]),
            TimeProvider.System);

    private static async Task<Guid> CreateAgreedOrderAsync()
    {
        var buyerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await using var connection = await OpenConnectionAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO identity.users
                ("Id", "Email", "PhoneNumber", "PasswordHash", "FullName", "Status", "CreatedAt")
            VALUES
                (@BuyerId, @Email, @PhoneNumber, 'test-hash', 'Payment Buyer', 'ACTIVE', @Now);

            INSERT INTO identity.buyer_profiles
                ("UserId", "CreatedAt")
            VALUES
                (@BuyerId, @Now);

            INSERT INTO sales.orders
                ("Id", "OrderNo", "BuyerId", "Status", "TotalToCharge", "TotalPaid",
                 "Currency", "PlacedAt", "CreatedAt")
            VALUES
                (@OrderId, @OrderNo, @BuyerId, 'AGREED', 300000, 0, 'VND', @Now, @Now);
            """,
            new
            {
                BuyerId = buyerId,
                Email = $"payment-{buyerId:N}@example.com",
                PhoneNumber = buyerId.ToString("N"),
                OrderId = orderId,
                OrderNo = $"ORD-{orderId:N}".ToUpperInvariant(),
                Now
            });
        return orderId;
    }

    private static HagglyDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<HagglyDbContext>()
            .UseNpgsql(IntegrationTestDatabase.ConnectionString)
            .Options);

    private static async Task<System.Data.Common.DbConnection> OpenConnectionAsync()
        => await new DapperDbContext(IntegrationTestDatabase.CreateConfiguration())
            .OpenConnectionAsync(CancellationToken.None);

    private sealed class FailingOutboxWriter : IOutboxWriter
    {
        public Task WriteAsync<TEvent>(
            TEvent domainEvent,
            CancellationToken cancellationToken = default)
            where TEvent : class, IDomainEvent
            => throw new InvalidOperationException("outbox unavailable");
    }

    private sealed class FixedBusinessClock(DateTimeOffset now) : IBusinessClock
    {
        public DateTimeOffset GetNow() => now;
        public DateOnly GetBusinessDate() => DateOnly.FromDateTime(now.DateTime);
    }
}
