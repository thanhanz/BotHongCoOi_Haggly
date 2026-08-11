using Haggly.Domain.Modules.Markets;

namespace Haggly.Application.Abstractions.Markets;

public interface IMarketCommandRepository
{
    Task<bool> CodeExistsAsync(
        string code,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<Market?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Market market, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
