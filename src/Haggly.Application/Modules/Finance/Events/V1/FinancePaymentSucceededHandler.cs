using Haggly.Application.Abstractions.Finance;
using Haggly.Application.Abstractions.Payments;
using Haggly.Application.Modules.Payments.Events.V1;
using Haggly.Domain.Modules.Finance;

namespace Haggly.Application.Modules.Finance.Events.V1;

public sealed class FinancePaymentSucceededHandler(
    IRevenueLedgerRepository revenueRepository,
    IPaymentAllocationRepository allocationRepository)
    : IFinancePaymentSucceededHandler
{
    public async Task ConsumeAsync(
        PaymentSucceededEvent paymentSucceededEvent,
        CancellationToken cancellationToken)
    {
        if (paymentSucceededEvent.PaymentAllocationIds.Count == 0
            || paymentSucceededEvent.PaymentAllocationIds.Distinct().Count()
            != paymentSucceededEvent.PaymentAllocationIds.Count)
        {
            throw new InvalidOperationException(
                "PaymentSucceededEvent must reference unique payment allocations.");
        }

        var allocations = await allocationRepository.FindByIdsAsync(
              paymentSucceededEvent.PaymentAllocationIds,
              cancellationToken);
        
        if (allocations.Count != paymentSucceededEvent.PaymentAllocationIds.Count
            || allocations.Sum(allocation => allocation.AllocatedAmount)
                != paymentSucceededEvent.Amount)
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
                paymentSucceededEvent.OccurredAt), cancellationToken);
            added = true;
        }

        if (added)
            await revenueRepository.SaveChangesAsync(cancellationToken);
    }
}
