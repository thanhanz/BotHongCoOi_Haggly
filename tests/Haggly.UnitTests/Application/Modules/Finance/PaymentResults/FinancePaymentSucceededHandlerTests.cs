using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Modules.Finance.Events.V1;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Modules.Payments;
using NSubstitute;
using Xunit;
using DomainRevenueLedger = Haggly.Domain.Modules.Finance.RevenueLedger;

namespace Haggly.UnitTests.Application.Modules.Finance.PaymentResults;

public sealed class FinancePaymentSucceededHandlerTests
{
    private readonly IRevenueLedgerRepository _revenue = Substitute.For<IRevenueLedgerRepository>();
    private readonly IPaymentAllocationRepository _allocations = Substitute.For<IPaymentAllocationRepository>();

    [Fact]
    public async Task HandleAsync_ValidAllocations_AddsAndSavesRevenueEntries()
    {
        // Arrange
        var first = CreateAllocation(Guid.Parse("F0000000-0000-0000-0000-000000000001"), 120_000m);
        var second = CreateAllocation(Guid.Parse("F0000000-0000-0000-0000-000000000002"), 180_000m);
        _allocations.FindByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns<IReadOnlyList<PaymentAllocation>>([first, second]);
        _revenue.ExistsForPaymentAllocationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var message = CreateMessage([first.Id, second.Id]);

        // Act
        await CreateSubject().HandleAsync(message, CancellationToken.None);

        // Assert
        await _revenue.Received(2).AddAsync(Arg.Any<DomainRevenueLedger>(), Arg.Any<CancellationToken>());
        await _revenue.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ExistingAllocation_SkipsDuplicateAndDoesNotSaveWhenAllExist()
    {
        // Arrange
        var allocation = CreateAllocation(Guid.Parse("F0000000-0000-0000-0000-000000000003"), 300_000m);
        _allocations.FindByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns<IReadOnlyList<PaymentAllocation>>([allocation]);
        _revenue.ExistsForPaymentAllocationAsync(allocation.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await CreateSubject().HandleAsync(CreateMessage([allocation.Id]), CancellationToken.None);

        // Assert
        await _revenue.DidNotReceive().AddAsync(Arg.Any<DomainRevenueLedger>(), Arg.Any<CancellationToken>());
        await _revenue.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private FinancePaymentSucceededHandler CreateSubject() => new(_revenue, _allocations);
    private static PaymentAllocation CreateAllocation(Guid id, decimal amount) => PaymentAllocation.CreateSale(id, Guid.Parse("F1000000-0000-0000-0000-000000000001"), Guid.Parse("F1000000-0000-0000-0000-000000000002"), Guid.Parse("F1000000-0000-0000-0000-000000000003"), amount, Now);
    private static PaymentSucceededEvent CreateMessage(IReadOnlyList<Guid> ids) => new(Guid.Parse("F2000000-0000-0000-0000-000000000001"), Guid.Parse("F2000000-0000-0000-0000-000000000002"), Now, Guid.Parse("F2000000-0000-0000-0000-000000000003"), Guid.Parse("F2000000-0000-0000-0000-000000000004"), Guid.Parse("F2000000-0000-0000-0000-000000000005"), 300_000m, "VND", "SIM-1", ids);
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 15, 0, 0, TimeSpan.Zero);
}
