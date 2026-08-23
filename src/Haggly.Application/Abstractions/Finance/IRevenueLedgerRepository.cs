using Haggly.Domain.Modules.Finance;

namespace Haggly.Application.Abstractions.Finance;

public interface IRevenueLedgerRepository
{
    Task AddAsync(
        RevenueLedger ledger,
        CancellationToken cancellationToken);
}
