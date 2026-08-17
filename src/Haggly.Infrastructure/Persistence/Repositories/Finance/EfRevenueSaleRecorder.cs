using Haggly.Application.Abstractions.Finance;
using Haggly.Domain.Modules.Finance;

namespace Haggly.Infrastructure.Persistence.Repositories.Finance;

public sealed class EfRevenueSaleRecorder(HagglyDbContext dbContext) : IRevenueSaleRecorder
{
    public Task RecordCompletedPosSaleAsync(
        CompletedPosSaleRevenue revenue,
        CancellationToken cancellationToken)
    {
        dbContext.RevenueLedgers.Add(RevenueLedger.CreatePosSaleEntry(
            revenue.SaleId,
            revenue.StallId,
            revenue.GrossAmount,
            revenue.OccurredAt));
        return Task.CompletedTask;
    }
}
