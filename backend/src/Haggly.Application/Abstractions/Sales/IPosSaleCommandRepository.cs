using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Abstractions.Sales;

public interface IPosSaleCommandRepository
{
    Task<PosSale?> FindByClientRequestIdAsync(
        Guid stallId,
        string clientRequestId,
        CancellationToken cancellationToken);

    Task AddAsync(PosSale sale, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
