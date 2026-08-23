using Haggly.Application.Abstractions.Finance;
using Haggly.Domain.Modules.Finance;

namespace Haggly.Infrastructure.Persistence.Repositories.Finance;

public sealed class EfRevenueLedgerRepository(HagglyDbContext dbContext)
    : IRevenueLedgerRepository
{
    public Task AddAsync(
        RevenueLedger ledger,
        CancellationToken cancellationToken)
    {
        dbContext.RevenueLedgers.Add(ledger);
        return Task.CompletedTask;
    }
}
