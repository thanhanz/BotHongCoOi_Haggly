using Haggly.Application.Abstractions.Finance;
using Haggly.Domain.Modules.Finance;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Finance;

public sealed class EfRevenueLedgerRepository(HagglyDbContext dbContext)
    : IRevenueLedgerRepository
{
    public Task<bool> ExistsForPaymentAllocationAsync(
        Guid paymentAllocationId,
        CancellationToken cancellationToken)
        => dbContext.RevenueLedgers.AnyAsync(
            ledger => ledger.PaymentAllocationId == paymentAllocationId
                && ledger.EntryType == RevenueEntryType.SALE,
            cancellationToken);

    public Task AddAsync(
        RevenueLedger ledger,
        CancellationToken cancellationToken)
    {
        dbContext.RevenueLedgers.Add(ledger);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
