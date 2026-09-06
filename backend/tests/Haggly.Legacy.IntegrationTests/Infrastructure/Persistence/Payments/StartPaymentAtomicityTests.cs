using Dapper;
using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Common.Time;
using Haggly.Application.Modules.Payments.Commands;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Application.Modules.Finance.Events.V1;
using Haggly.Application.Modules.Inventory.Events.V1;
using Haggly.Application.Modules.Inventory.Exceptions;
using Haggly.Application.Modules.Sales.Events.V1;
using Haggly.Domain.Common.Events.V1;
using Haggly.Domain.Modules.Payments;
using Haggly.Infrastructure.Messaging.Outbox;
using Haggly.Infrastructure.Messaging.Inbox;
using Haggly.Infrastructure.Messaging.Serialization;
using Haggly.Infrastructure.Persistence;
using Haggly.Infrastructure.Persistence.Repositories.Payments;
using Haggly.Infrastructure.Persistence.Repositories.Inventory;
using Haggly.Infrastructure.Persistence.Repositories.Finance;
using Haggly.Infrastructure.Persistence.Repositories.Sales;
using Haggly.Infrastructure.Persistence.Transactions.Sales;
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
        Assert.Equal(5m, await GetReservedQuantityAsync(connection, orderId));
        Assert.Equal("PAYMENT_PENDING", await GetOrderStatusAsync(connection, orderId));
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
        Assert.Equal(0m, await GetReservedQuantityAsync(connection, orderId));
    }

    [Fact]
    public async Task Handle_WhenAnyOrderItemLacksStock_RollsBackEveryReservationAndPaymentRecord()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using (var connection = await OpenConnectionAsync())
        {
            await connection.ExecuteAsync(
                """
                UPDATE inventory.inventory_items AS i
                SET "CurrentQuantity" = 1
                FROM sales.order_items AS oi
                JOIN sales.stall_fulfillments AS sf ON sf."Id" = oi."StallFulfillmentId"
                WHERE i."Id" = oi."InventoryItemId"
                  AND sf."OrderId" = @OrderId
                  AND oi."FinalQuantity" = 3;
                """,
                new { OrderId = orderId });
        }
        await using var dbContext = CreateDbContext();
        var handler = CreateHandler(dbContext, CreateWriter(dbContext));

        await Assert.ThrowsAsync<InventoryConflictException>(() =>
            handler.Handle(new StartPaymentCommand(orderId, buyerId), CancellationToken.None));

        await using var verificationConnection = await OpenConnectionAsync();
        Assert.Equal(0, await verificationConnection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM payments.payments WHERE \"OrderId\" = @OrderId;",
            new { OrderId = orderId }));
        Assert.Equal(0m, await GetReservedQuantityAsync(verificationConnection, orderId));
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
        Assert.Equal(5m, await GetReservedQuantityAsync(connection, orderId));
    }

    [Fact]
    public async Task InventoryHandleAsync_WhenPaymentSucceeds_ConsumesReservedStockOnce()
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
        var transactionId = await dbContext.PaymentTransactions
            .Where(transaction => transaction.PaymentId == payment.Id)
            .Select(transaction => transaction.Id)
            .SingleAsync();
        var integrationEvent = new PaymentSucceededEvent(
            Guid.NewGuid(), payment.Id, Now, payment.Id, transactionId, orderId,
            payment.AmountDue, payment.Currency, "SIM-INVENTORY",
            await dbContext.PaymentAllocations
                .Where(allocation => allocation.PaymentTransactionId == transactionId)
                .Select(allocation => allocation.Id)
                .ToArrayAsync());
        var inventoryHandler = new InventoryPaymentSucceededHandler(
            new EfInventoryPaymentRepository(dbContext));

        await inventoryHandler.HandleAsync(integrationEvent, CancellationToken.None);
        dbContext.ChangeTracker.Clear();
        await inventoryHandler.HandleAsync(integrationEvent, CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(0m, await GetReservedQuantityAsync(connection, orderId));
        Assert.Equal(15m, await connection.ExecuteScalarAsync<decimal>(
            """
            SELECT SUM(i."CurrentQuantity")
            FROM inventory.inventory_items AS i
            JOIN sales.order_items AS oi ON oi."InventoryItemId" = i."Id"
            JOIN sales.stall_fulfillments AS sf ON sf."Id" = oi."StallFulfillmentId"
            WHERE sf."OrderId" = @OrderId;
            """,
            new { OrderId = orderId }));
        Assert.Equal(2, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM inventory.inventory_ledgers AS l
            WHERE l."ReferenceId" = @TransactionId AND l."TransactionType" = 'ONLINE_SALE';
            """,
            new { TransactionId = transactionId }));
    }

    [Fact]
    public async Task InventoryFailureHandleAsync_WhenEventIsNew_ReleasesReservationAndStoresInboxMessage()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var payment = await CreateHandler(dbContext, CreateWriter(dbContext)).Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        var integrationEvent = CreateFailedEvent(payment.Id, orderId);
        var handler = CreateInventoryFailureHandler(dbContext);

        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(0m, await GetReservedQuantityAsync(connection, orderId));
        Assert.Equal(1, await GetInboxCountAsync(connection, integrationEvent.EventId));
    }

    [Fact]
    public async Task InventoryFailureHandleAsync_WhenEventIsDuplicate_DoesNotReleaseReservationTwice()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var payment = await CreateHandler(dbContext, CreateWriter(dbContext)).Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        var integrationEvent = CreateFailedEvent(payment.Id, orderId);
        var handler = CreateInventoryFailureHandler(dbContext);

        await handler.HandleAsync(integrationEvent, CancellationToken.None);
        dbContext.ChangeTracker.Clear();
        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(0m, await GetReservedQuantityAsync(connection, orderId));
        Assert.Equal(1, await GetInboxCountAsync(connection, integrationEvent.EventId));
    }

    [Fact]
    public async Task InventoryFailureHandleAsync_WhenReleaseFails_RollsBackInboxMessage()
    {
        var (orderId, _) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var integrationEvent = CreateFailedEvent(Guid.NewGuid(), orderId);
        var handler = CreateInventoryFailureHandler(dbContext);

        await Assert.ThrowsAsync<InventoryConflictException>(() =>
            handler.HandleAsync(integrationEvent, CancellationToken.None));

        await using var connection = await OpenConnectionAsync();
        Assert.Equal(0, await GetInboxCountAsync(connection, integrationEvent.EventId));
    }

    [Fact]
    public async Task OrderFailureHandleAsync_WhenEventIsNew_MovesOrderToAgreedAndStoresInboxMessage()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var payment = await CreateHandler(dbContext, CreateWriter(dbContext)).Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        var integrationEvent = CreateFailedEvent(payment.Id, orderId);
        var handler = CreateOrderFailureHandler(dbContext);

        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        Assert.Equal("AGREED", await GetOrderStatusAsync(connection, orderId));
        Assert.Equal(1, await GetOrderFailureInboxCountAsync(
            connection,
            integrationEvent.EventId));
    }

    [Fact]
    public async Task OrderFailureHandleAsync_WhenEventIsDuplicate_DoesNotChangeOrderAgain()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var payment = await CreateHandler(dbContext, CreateWriter(dbContext)).Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        var integrationEvent = CreateFailedEvent(payment.Id, orderId);
        var handler = CreateOrderFailureHandler(dbContext);

        await handler.HandleAsync(integrationEvent, CancellationToken.None);
        dbContext.ChangeTracker.Clear();
        await handler.HandleAsync(integrationEvent, CancellationToken.None);

        await using var connection = await OpenConnectionAsync();
        Assert.Equal("AGREED", await GetOrderStatusAsync(connection, orderId));
        Assert.Equal(1, await GetOrderFailureInboxCountAsync(
            connection,
            integrationEvent.EventId));
    }

    [Fact]
    public async Task OrderFailureHandleAsync_WhenOrderUpdateFails_RollsBackInboxMessage()
    {
        var (orderId, buyerId) = await CreateAgreedOrderAsync();
        await using var dbContext = CreateDbContext();
        var payment = await CreateHandler(dbContext, CreateWriter(dbContext)).Handle(
            new StartPaymentCommand(orderId, buyerId),
            CancellationToken.None);
        var integrationEvent = CreateFailedEvent(payment.Id, orderId) with { Amount = 1m };
        var handler = CreateOrderFailureHandler(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(integrationEvent, CancellationToken.None));

        await using var connection = await OpenConnectionAsync();
        Assert.Equal("PAYMENT_PENDING", await GetOrderStatusAsync(connection, orderId));
        Assert.Equal(0, await GetOrderFailureInboxCountAsync(
            connection,
            integrationEvent.EventId));
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
            new EfOrderCommandRepository(dbContext),
            new EfInventoryPaymentRepository(dbContext),
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

    private static InventoryPaymentFailedHandler CreateInventoryFailureHandler(
        HagglyDbContext dbContext)
        => new(
            new DapperInboxRepository(dbContext),
            new EfInventoryPaymentRepository(dbContext),
            new EfInventoryUnitOfWork(dbContext),
            new FixedBusinessClock(Now.AddMinutes(1)));

    private static OrderPaymentFailedHandler CreateOrderFailureHandler(
        HagglyDbContext dbContext)
        => new(
            new EfOrderCommandRepository(dbContext),
            new DapperInboxRepository(dbContext),
            new EfSalesTransactionExecutor(dbContext),
            new FixedBusinessClock(Now.AddMinutes(1)));

    private static PaymentRequested CreateRequested(Guid paymentId, Guid orderId)
        => new(
            Guid.NewGuid(),
            paymentId,
            Now,
            paymentId,
            orderId,
            300_000m,
            "VND");

    private static PaymentFailedEvent CreateFailedEvent(Guid paymentId, Guid orderId)
        => new(
            Guid.NewGuid(),
            paymentId,
            Now,
            paymentId,
            Guid.NewGuid(),
            orderId,
            300_000m,
            "VND",
            "simulated decline");

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
        var categoryId = Guid.NewGuid();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var firstProductStallId = Guid.NewGuid();
        var secondProductStallId = Guid.NewGuid();
        var firstInventoryId = Guid.NewGuid();
        var secondInventoryId = Guid.NewGuid();
        var firstInventoryItemId = Guid.NewGuid();
        var secondInventoryItemId = Guid.NewGuid();
        var firstOrderItemId = Guid.NewGuid();
        var secondOrderItemId = Guid.NewGuid();
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

            INSERT INTO inventory.inventories
                ("Id", "StallId", "CreatedAt")
            VALUES
                (@FirstInventoryId, @FirstStallId, @Now),
                (@SecondInventoryId, @SecondStallId, @Now);

            INSERT INTO catalog.categories
                ("Id", "Name", "Slug", "DisplayOrder", "Status", "CreatedAt")
            VALUES
                (@CategoryId, 'Payment Category', @CategorySlug, 0, 'ACTIVE', @Now);

            INSERT INTO catalog.products
                ("Id", "CategoryId", "Name", "DefaultUnit", "Status", "CreatedAt")
            VALUES
                (@FirstProductId, @CategoryId, @FirstProductName, 'KG', 'ACTIVE', @Now),
                (@SecondProductId, @CategoryId, @SecondProductName, 'KG', 'ACTIVE', @Now);

            INSERT INTO catalog.product_stalls
                ("Id", "StallId", "ProductId", "DisplayName", "SellingUnit",
                 "MinimumOrderQuantity", "CurrentUnitPrice", "IsNegotiable", "IsActive", "Version", "CreatedAt")
            VALUES
                (@FirstProductStallId, @FirstStallId, @FirstProductId, 'Payment Product One', 'KG',
                 1, 60000, FALSE, TRUE, 0, @Now),
                (@SecondProductStallId, @SecondStallId, @SecondProductId, 'Payment Product Two', 'KG',
                 1, 60000, FALSE, TRUE, 0, @Now);

            INSERT INTO inventory.inventory_items
                ("Id", "InventoryId", "ProductStallId", "CurrentQuantity", "ReservedQuantity", "Version", "CreatedAt")
            VALUES
                (@FirstInventoryItemId, @FirstInventoryId, @FirstProductStallId, 10, 0, 0, @Now),
                (@SecondInventoryItemId, @SecondInventoryId, @SecondProductStallId, 10, 0, 0, @Now);

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

            INSERT INTO sales.order_items
                ("Id", "StallFulfillmentId", "InventoryItemId", "ProductNameSnapshot",
                 "SellingUnitSnapshot", "PublicUnitPriceSnapshot", "FinalUnitPrice",
                 "FinalQuantity", "LineTotal", "IsNegotiated", "Status", "CreatedAt")
            VALUES
                (@FirstOrderItemId, @FirstFulfillmentId, @FirstInventoryItemId, 'Payment Product One',
                 'KG', 60000, 60000, 2, 120000, FALSE, 'ACTIVE', @Now),
                (@SecondOrderItemId, @SecondFulfillmentId, @SecondInventoryItemId, 'Payment Product Two',
                 'KG', 60000, 60000, 3, 180000, FALSE, 'ACTIVE', @Now);
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
                CategoryId = categoryId,
                CategorySlug = $"payment-{categoryId:N}",
                FirstProductId = firstProductId,
                SecondProductId = secondProductId,
                FirstProductName = $"Payment-{firstProductId:N}",
                SecondProductName = $"Payment-{secondProductId:N}",
                FirstProductStallId = firstProductStallId,
                SecondProductStallId = secondProductStallId,
                FirstInventoryId = firstInventoryId,
                SecondInventoryId = secondInventoryId,
                FirstInventoryItemId = firstInventoryItemId,
                SecondInventoryItemId = secondInventoryItemId,
                FirstOrderItemId = firstOrderItemId,
                SecondOrderItemId = secondOrderItemId,
                OrderId = orderId,
                OrderNo = $"ORD-{orderId:N}".ToUpperInvariant(),
                Now
            });
        return (orderId, buyerId);
    }

    private static Task<decimal> GetReservedQuantityAsync(
        System.Data.Common.DbConnection connection,
        Guid orderId)
        => connection.ExecuteScalarAsync<decimal>(
            """
            SELECT SUM(i."ReservedQuantity")
            FROM inventory.inventory_items AS i
            JOIN sales.order_items AS oi ON oi."InventoryItemId" = i."Id"
            JOIN sales.stall_fulfillments AS sf ON sf."Id" = oi."StallFulfillmentId"
            WHERE sf."OrderId" = @OrderId;
            """,
            new { OrderId = orderId });

    private static Task<int> GetInboxCountAsync(
        System.Data.Common.DbConnection connection,
        Guid eventId)
        => connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM messaging.inbox_messages
            WHERE "ConsumerName" = 'inventory-payment-failed-v1'
              AND "EventId" = @EventId;
            """,
            new { EventId = eventId });

    private static Task<int> GetOrderFailureInboxCountAsync(
        System.Data.Common.DbConnection connection,
        Guid eventId)
        => connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM messaging.inbox_messages
            WHERE "ConsumerName" = 'order-payment-failed-v1'
              AND "EventId" = @EventId;
            """,
            new { EventId = eventId });

    private static Task<string?> GetOrderStatusAsync(
        System.Data.Common.DbConnection connection,
        Guid orderId)
        => connection.ExecuteScalarAsync<string>(
            "SELECT \"Status\" FROM sales.orders WHERE \"Id\" = @OrderId;",
            new { OrderId = orderId });

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
