using Haggly.Domain.Modules.Finance;

namespace Haggly.Application.Abstractions.Finance;

public interface IRevenueLedgerRepository
{
    Task<bool> ExistsForPaymentAllocationAsync(
        Guid paymentAllocationId,
        CancellationToken cancellationToken);

    Task AddAsync(
        RevenueLedger ledger,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
