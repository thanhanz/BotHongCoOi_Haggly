using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Modules.Finance.Events.V1;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Modules.Finance;
using Haggly.Domain.Modules.Payments;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Finance;

public sealed class FinancePaymentSucceededHandlerTests
{
    [Fact]
    public async Task ConsumeAsync_WhenAllocationsAreValid_AppendsOneLedgerPerStall()
    {
        var allocations = CreateAllocations();
        var revenue = new FakeRevenueLedgerRepository();
        var handler = new FinancePaymentSucceededHandler(
            revenue,
            new FakePaymentAllocationRepository(allocations));
        var integrationEvent = CreateEvent(allocations);

        await handler.ConsumeAsync(integrationEvent, CancellationToken.None);

        Assert.Equal(2, revenue.Entries.Count);
        Assert.Equal(integrationEvent.Amount, revenue.Entries.Sum(entry => entry.NetAmount));
        Assert.Equal(1, revenue.SaveCount);
    }

    [Fact]
    public async Task ConsumeAsync_WhenAllocationWasAlreadyRecorded_DoesNotAppendDuplicate()
    {
        var allocations = CreateAllocations();
        var revenue = new FakeRevenueLedgerRepository
        {
            ExistingAllocationId = allocations[0].Id
        };
        var handler = new FinancePaymentSucceededHandler(
            revenue,
            new FakePaymentAllocationRepository(allocations));

        await handler.ConsumeAsync(CreateEvent(allocations), CancellationToken.None);

        var entry = Assert.Single(revenue.Entries);
        Assert.Equal(allocations[1].Id, entry.PaymentAllocationId);
    }

    private static PaymentSucceeded CreateEvent(IReadOnlyList<PaymentAllocation> allocations)
        => new(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 300_000m, "VND", "SIM-1",
            allocations.Select(allocation => allocation.Id).ToArray());

    private static PaymentAllocation[] CreateAllocations()
    {
        var transactionId = Guid.NewGuid();
        return
        [
            PaymentAllocation.CreateSale(
                Guid.NewGuid(), transactionId, Guid.NewGuid(), Guid.NewGuid(),
                120_000m, DateTimeOffset.UtcNow),
            PaymentAllocation.CreateSale(
                Guid.NewGuid(), transactionId, Guid.NewGuid(), Guid.NewGuid(),
                180_000m, DateTimeOffset.UtcNow)
        ];
    }

    private sealed class FakePaymentAllocationRepository(
        IReadOnlyList<PaymentAllocation> allocations) : IPaymentAllocationRepository
    {
        public Task<IReadOnlyList<PaymentAllocationTarget>> GetTargetsForOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PaymentAllocationTarget>>([]);

        public Task<IReadOnlyList<PaymentAllocation>> FindByIdsAsync(
            IReadOnlyCollection<Guid> allocationIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PaymentAllocation>>(
                allocations.Where(item => allocationIds.Contains(item.Id)).ToArray());

        public Task AddAsync(PaymentAllocation allocation, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeRevenueLedgerRepository : IRevenueLedgerRepository
    {
        public Guid? ExistingAllocationId { get; init; }
        public List<RevenueLedger> Entries { get; } = [];
        public int SaveCount { get; private set; }

        public Task<bool> ExistsForPaymentAllocationAsync(
            Guid paymentAllocationId,
            CancellationToken cancellationToken)
            => Task.FromResult(ExistingAllocationId == paymentAllocationId);

        public Task AddAsync(RevenueLedger ledger, CancellationToken cancellationToken)
        {
            Entries.Add(ledger);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
