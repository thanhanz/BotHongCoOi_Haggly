using Haggly.Domain.Modules.Payments;

namespace Haggly.Application.Abstractions.Payments;

public interface IPaymentAllocationRepository
{
    Task<IReadOnlyList<PaymentAllocationTarget>> GetTargetsForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentAllocation>> FindByIdsAsync(
        IReadOnlyCollection<Guid> allocationIds,
        CancellationToken cancellationToken);

    Task AddAsync(
        PaymentAllocation allocation,
        CancellationToken cancellationToken);
}

public sealed record PaymentAllocationTarget(
    Guid StallFulfillmentId,
    Guid StallId,
    decimal Amount);
