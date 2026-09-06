using Haggly.Domain.Modules.Markets;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.Application.Abstractions.Markets;

public interface IStallCommandRepository
{
    Task<bool> CodeExistsAsync(
        Guid marketId,
        string code,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> MarketExistsAsync(Guid marketId, CancellationToken cancellationToken);

    Task<bool> VendorExistsAsync(Guid vendorId, CancellationToken cancellationToken);

    Task<Stall?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Stall stall, CancellationToken cancellationToken);

    Task AddInventoryAsync(DomainInventory inventory, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
