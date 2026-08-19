using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Abstractions.Sales;

public interface ICartCommandRepository
{
    Task<Cart?> FindByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken);

    Task AddAsync(Cart cart, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
