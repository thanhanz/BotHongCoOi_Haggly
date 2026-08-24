using Dapper;
using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Commands;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Finance.Events.V1;
using Haggly.Application.Modules.Sales.Events.V1;
using Haggly.Domain.Common.Events.V1;
using Haggly.Domain.Modules.Payments;
using Haggly.Infrastructure.Messaging.Outbox;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Repositories.Payments;
using Haggly.Infrastructure.Persistence.Repositories.Finance;
using Haggly.Infrastructure.Persistence.Repositories.Sales;
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
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext, CreateWriter(dbContext));

        var result = await handler.Handle(new StartPaymentCommand(orderId, buyerId), CancellationToken.None);

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
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext, new FailingOutboxWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new StartPaymentCommand(orderId, buyerId), CancellationToken.None));

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM payments.payments WHERE \"OrderId\" = @OrderId;",
            new { OrderId = orderId }));
    }

    [Fact]
    public async Task SaveChanges_WhenPaymentTransactionIsPending_RoundTripsThroughPostgreSql()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext, CreateWriter(dbContext));
        var paymentResult = await handler.Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        var payment = await dbContext.Payments.SingleAsync(item => item.Id == paymentResult.Id);
        var transactionId = Guid.NewGuid();
        var transaction = PaymentTransaction.Create(
            transactionId,
            payment,
            payment.AmountDue,
            Now.AddMinutes(1));

        dbContext.PaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var stored = await dbContext.PaymentTransactions
            .AsNoTracking()
            .SingleAsync(item => item.Id == transactionId);
        Assert.Equal(payment.Id, stored.PaymentId);
        Assert.Equal(PaymentTransactionType.PAYMENT, stored.TransactionType);
        Assert.Equal(PaymentTransactionStatus.PENDING, stored.Status);
        Assert.Equal(payment.AmountDue, stored.Amount);
        Assert.Equal(TimeSpan.Zero, stored.CreatedAt.Offset);
    }

    [Theory]
    [InlineData(true, "PAID", "SUCCEEDED", "payments.payment-succeeded.v1", 2)]
    [InlineData(false, "FAILED", "FAILED", "payments.payment-failed.v1", 0)]
    public async Task ConsumeAsync_WhenProviderReturnsResult_CommitsPaymentAttemptAndOutboxAtomically(
        bool providerSucceeded,
        string expectedPaymentStatus,
        string expectedTransactionStatus,
        string expectedEventType,
        int expectedAllocationCount)
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var startHandler = CreateHandler(dbContext, CreateWriter(dbContext));
        var payment = await startHandler.Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        var requested = CreateRequested(payment.Id, orderId);
        var consumer = CreateProcessingConsumer(
            dbContext,
            CreateWriter(dbContext),
            new FixedPaymentProvider(providerSucceeded));

        await consumer.HandleAsync(requested, CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(expectedPaymentStatus, await connection.ExecuteScalarAsync<string>(
            "SELECT \"Status\" FROM payments.payments WHERE \"Id\" = @PaymentId;",
            new { PaymentId = payment.Id }));
        Assert.Equal(expectedTransactionStatus, await connection.ExecuteScalarAsync<string>(
            "SELECT \"Status\" FROM payments.payment_transactions WHERE \"PaymentId\" = @PaymentId;",
            new { PaymentId = payment.Id }));
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM messaging.outbox_messages
            WHERE "CorrelationId" = @PaymentId AND "EventType" = @EventType;
            """,
            new { PaymentId = payment.Id, EventType = expectedEventType }));
        Assert.Equal(expectedAllocationCount, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM payments.payment_allocations AS a
            JOIN payments.payment_transactions AS t ON t."Id" = a."PaymentTransactionId"
            WHERE t."PaymentId" = @PaymentId;
            """,
            new { PaymentId = payment.Id }));
    }

    [Fact]
    public async Task ConsumeAsync_WhenResultOutboxWriteFails_RollsBackPaymentAndAttempt()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var startHandler = CreateHandler(dbContext, CreateWriter(dbContext));
        var payment = await startHandler.Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        var consumer = CreateProcessingConsumer(
            dbContext,
            new FailingOutboxWriter(),
            new FixedPaymentProvider(true));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            consumer.HandleAsync(CreateRequested(payment.Id, orderId), CancellationToken.None));

        await using var connection = await OpenConnectionAsync();
        Assert.Equal("PENDING", await connection.ExecuteScalarAsync<string>(
            "SELECT \"Status\" FROM payments.payments WHERE \"Id\" = @PaymentId;",
            new { PaymentId = payment.Id }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM payments.payment_transactions WHERE \"PaymentId\" = @PaymentId;",
            new { PaymentId = payment.Id }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM payments.payment_allocations AS a
            JOIN payments.payment_transactions AS t ON t."Id" = a."PaymentTransactionId"
            WHERE t."PaymentId" = @PaymentId;
            """,
            new { PaymentId = payment.Id }));
    }

    [Fact]
    public async Task FinanceConsumeAsync_WhenDeliveredTwice_AppendsOneRevenueEntryPerAllocation()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var payment = await CreateHandler(dbContext, CreateWriter(dbContext)).Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        await CreateProcessingConsumer(
            dbContext,
            CreateWriter(dbContext),
            new FixedPaymentProvider(true)).HandleAsync(
                CreateRequested(payment.Id, orderId),
                CancellationToken.None);
        var allocations = await dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(allocation => allocation.PaymentTransaction!.PaymentId == payment.Id)
            .ToArrayAsync();
        var integrationEvent = new PaymentSucceededEvent(
            Guid.NewGuid(), payment.Id, Now, payment.Id,
            allocations.Select(_ => Guid.NewGuid()).First(), orderId,
            payment.AmountDue, payment.Currency, "SIM-FINANCE",
            allocations.Select(allocation => allocation.Id).ToArray());
        var financeHandler = new FinancePaymentSucceededHandler(
            new EfRevenueLedgerRepository(dbContext),
            new EfPaymentAllocationRepository(dbContext));

        await financeHandler.HandleAsync(integrationEvent, CancellationToken.None);
        await financeHandler.HandleAsync(integrationEvent, CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(2, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM finance.revenue_ledgers
            WHERE "PaymentAllocationId" = ANY(@AllocationIds);
            """,
            new { AllocationIds = allocations.Select(item => item.Id).ToArray() }));
    }

    [Fact]
    public async Task OrderHandleAsync_WhenDeliveredTwice_MarksOrderAndFulfillmentsPaidOnce()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var payment = await CreateHandler(dbContext, CreateWriter(dbContext)).Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        await CreateProcessingConsumer(
            dbContext,
            CreateWriter(dbContext),
            new FixedPaymentProvider(true)).HandleAsync(
                CreateRequested(payment.Id, orderId),
                CancellationToken.None);
        var allocations = await dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(allocation => allocation.PaymentTransaction!.PaymentId == payment.Id)
            .ToArrayAsync();
        var transactionId = allocations.Select(allocation => allocation.PaymentTransactionId).Distinct().Single();
        var integrationEvent = new PaymentSucceededEvent(
            Guid.NewGuid(), payment.Id, Now, payment.Id,
            transactionId, orderId, payment.AmountDue, payment.Currency, "SIM-ORDER",
            allocations.Select(allocation => allocation.Id).ToArray());
        dbContext.ChangeTracker.Clear();
        var orderHandler = new OrderPaymentSucceededHandler(
            new EfOrderCommandRepository(dbContext),
            new EfPaymentAllocationRepository(dbContext));

        await orderHandler.HandleAsync(integrationEvent, CancellationToken.None);
        dbContext.ChangeTracker.Clear();
        await orderHandler.HandleAsync(integrationEvent, CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        var order = await connection.QuerySingleAsync<(string Status, decimal TotalPaid)>(
            """
            SELECT "Status", "TotalPaid"
            FROM sales.orders
            WHERE "Id" = @OrderId;
            """,
            new { OrderId = orderId });
        Assert.Equal("PAID", order.Status);
        Assert.Equal(payment.AmountDue, order.TotalPaid);
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM sales.stall_fulfillments
            WHERE "OrderId" = @OrderId
              AND ("PaidAmount" <> "FinalAmount" OR "Status" <> 'AGREED');
            """,
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
                DomainEventTypeRegistration.For<PaymentRequested>("payments.payment-requested.v1"),
                DomainEventTypeRegistration.For<PaymentSucceededEvent>("payments.payment-succeeded.v1"),
                DomainEventTypeRegistration.For<PaymentFailedEvent>("payments.payment-failed.v1")
            ]),
            TimeProvider.System);

    private static ProcessPaymentRequestedHandler CreateProcessingConsumer(
        HagglyDbContext dbContext,
        IOutboxWriter outboxWriter,
        IPaymentProvider paymentProvider)
        => new(
            new EfPaymentCommandRepository(dbContext),
            paymentProvider,
            new EfPaymentAllocationRepository(dbContext),
            outboxWriter,
            new EfPaymentUnitOfWork(dbContext),
            new FixedBusinessClock(Now));

    private static PaymentRequested CreateRequested(Guid paymentId, Guid orderId)
        => new(
            Guid.NewGuid(),
            paymentId,
            Now,
            paymentId,
            orderId,
            300_000m,
            "VND");

    private static async Task<(Guid OrderId, Guid BuyerId)> CreateAgreedOrderAsync()
    {
        var buyerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var marketId = Guid.NewGuid();
        var firstVendorId = Guid.NewGuid();
        var secondVendorId = Guid.NewGuid();
        var firstStallId = Guid.NewGuid();
        var secondStallId = Guid.NewGuid();
        var firstFulfillmentId = Guid.NewGuid();
        var secondFulfillmentId = Guid.NewGuid();
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

            INSERT INTO identity.users
                ("Id", "Email", "PhoneNumber", "PasswordHash", "FullName", "Status", "CreatedAt")
            VALUES
                (@FirstVendorId, @FirstVendorEmail, @FirstVendorPhone, 'test-hash', 'Vendor One', 'ACTIVE', @Now),
                (@SecondVendorId, @SecondVendorEmail, @SecondVendorPhone, 'test-hash', 'Vendor Two', 'ACTIVE', @Now);

            INSERT INTO identity.vendor_profiles
                ("UserId", "BusinessName", "ApprovalStatus", "CreatedAt")
            VALUES
                (@FirstVendorId, 'Vendor One', 'APPROVED', @Now),
                (@SecondVendorId, 'Vendor Two', 'APPROVED', @Now);

            INSERT INTO markets.markets
                ("Id", "Code", "Name", "Address", "Status", "CreatedAt")
            VALUES
                (@MarketId, @MarketCode, 'Payment Test Market', 'Test Address', 'ACTIVE', @Now);

            INSERT INTO markets.stalls
                ("Id", "MarketId", "VendorId", "Code", "Name", "Status", "CreatedAt")
            VALUES
                (@FirstStallId, @MarketId, @FirstVendorId, @FirstStallCode, 'Payment Stall One', 'ACTIVE', @Now),
                (@SecondStallId, @MarketId, @SecondVendorId, @SecondStallCode, 'Payment Stall Two', 'ACTIVE', @Now);

            INSERT INTO sales.orders
                ("Id", "OrderNo", "BuyerId", "Status", "TotalToCharge", "TotalPaid",
                 "Currency", "PlacedAt", "CreatedAt")
            VALUES
                (@OrderId, @OrderNo, @BuyerId, 'AGREED', 300000, 0, 'VND', @Now, @Now);

            INSERT INTO sales.stall_fulfillments
                ("Id", "OrderId", "StallId", "FulfillmentNo", "Status", "Subtotal",
                 "FinalAmount", "PaidAmount", "CreatedAt")
            VALUES
                (@FirstFulfillmentId, @OrderId, @FirstStallId, @FirstFulfillmentNo,
                 'AGREED', 120000, 120000, 0, @Now),
                (@SecondFulfillmentId, @OrderId, @SecondStallId, @SecondFulfillmentNo,
                 'AGREED', 180000, 180000, 0, @Now);
            """,
            new
            {
                BuyerId = buyerId,
                Email = $"payment-{buyerId:N}@example.com",
                PhoneNumber = buyerId.ToString("N"),
                FirstVendorId = firstVendorId,
                SecondVendorId = secondVendorId,
                FirstVendorEmail = $"vendor-{firstVendorId:N}@example.com",
                SecondVendorEmail = $"vendor-{secondVendorId:N}@example.com",
                FirstVendorPhone = firstVendorId.ToString("N"),
                SecondVendorPhone = secondVendorId.ToString("N"),
                MarketId = marketId,
                MarketCode = $"MKT-{marketId:N}".ToUpperInvariant(),
                FirstStallId = firstStallId,
                SecondStallId = secondStallId,
                FirstStallCode = $"S-{firstStallId:N}".ToUpperInvariant(),
                SecondStallCode = $"S-{secondStallId:N}".ToUpperInvariant(),
                FirstFulfillmentId = firstFulfillmentId,
                SecondFulfillmentId = secondFulfillmentId,
                FirstFulfillmentNo = $"FUL-{firstFulfillmentId:N}".ToUpperInvariant(),
                SecondFulfillmentNo = $"FUL-{secondFulfillmentId:N}".ToUpperInvariant(),
                OrderId = orderId,
                OrderNo = $"ORD-{orderId:N}".ToUpperInvariant(),
                Now
            });
        return (orderId, buyerId);
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

    private sealed class FixedPaymentProvider(bool succeeds) : IPaymentProvider
    {
        public Task<PaymentProviderResult> ProcessAsync(
            PaymentProviderRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(succeeds
                ? new PaymentProviderResult(true, $"SIM-{request.PaymentTransactionId:N}", null)
                : new PaymentProviderResult(false, null, "simulated decline"));
    }

    private sealed class FixedBusinessClock(DateTimeOffset now) : IBusinessClock
    {
        public DateTimeOffset GetNow() => now;
        public DateOnly GetBusinessDate() => DateOnly.FromDateTime(now.DateTime);
    }
}
