using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Common.Messaging;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Modules.Finance;

namespace Haggly.Application.Modules.Finance.Events.V1;

public sealed class FinancePaymentSucceededHandler(
    IRevenueLedgerRepository revenueRepository,
    IPaymentAllocationRepository allocationRepository)
    : IEventHandler<PaymentSucceededEvent>
{
    public async Task HandleAsync(
        PaymentSucceededEvent message,
        CancellationToken cancellationToken)
    {
        if (message.PaymentAllocationIds.Count == 0
            || message.PaymentAllocationIds.Distinct().Count()
            != message.PaymentAllocationIds.Count)
        {
            throw new InvalidOperationException(
                "PaymentSucceededEvent must reference unique payment allocations.");
        }

        var allocations = await allocationRepository.FindByIdsAsync(
              message.PaymentAllocationIds,
              cancellationToken);
        
        if (allocations.Count != message.PaymentAllocationIds.Count
            || allocations.Sum(allocation => allocation.AllocatedAmount)
                != message.Amount)
        {
            throw new InvalidOperationException(
                "Payment allocations must exist and equal the successful payment amount.");
        }

        var added = false;
        
        //For optimization, consider using bulk insert or batch operations
        foreach (var allocation in allocations)
        {
            if (await revenueRepository.ExistsForPaymentAllocationAsync(
                allocation.Id,
                cancellationToken))
            {
                continue;
            }

            await revenueRepository.AddAsync(RevenueLedger.CreatePaymentSaleEntry(
                allocation.Id,
                allocation.StallFulfillmentId,
                allocation.StallId,
                allocation.AllocatedAmount,
                message.OccurredAt), cancellationToken);
            added = true;
        }

        if (added)
            await revenueRepository.SaveChangesAsync(cancellationToken);
    }
}
