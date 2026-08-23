using Haggly.Application.Abstractions.Payments;
using Haggly.Domain.Modules.Payments;
using Haggly.Domain.Modules.Sales;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Payments;

public sealed class EfPaymentAllocationRepository(HagglyDbContext dbContext)
    : IPaymentAllocationRepository
{
    public async Task<IReadOnlyList<PaymentAllocationTarget>> GetTargetsForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
        => await dbContext.StallFulfillments
            .Where(fulfillment => fulfillment.OrderId == orderId
                && fulfillment.Status != StallFulfillmentStatus.CANCELLED
                && fulfillment.FinalAmount > 0m)
            .OrderBy(fulfillment => fulfillment.Id)
            .Select(fulfillment => new PaymentAllocationTarget(
                fulfillment.Id,
                fulfillment.StallId,
                fulfillment.FinalAmount))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentAllocation>> FindByIdsAsync(
        IReadOnlyCollection<Guid> allocationIds,
        CancellationToken cancellationToken)
        => await dbContext.PaymentAllocations
            .Where(allocation => allocationIds.Contains(allocation.Id))
            .ToArrayAsync(cancellationToken);

    public Task AddAsync(
        PaymentAllocation allocation,
        CancellationToken cancellationToken)
    {
        dbContext.PaymentAllocations.Add(allocation);
        return Task.CompletedTask;
    }
}
