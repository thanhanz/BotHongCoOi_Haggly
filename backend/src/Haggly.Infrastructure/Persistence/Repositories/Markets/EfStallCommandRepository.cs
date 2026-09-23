using Haggly.Application.Abstractions.Markets;
using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Markets;
using Microsoft.EntityFrameworkCore;
using DomainInventory = Haggly.Domain.Modules.Inventory.Inventory;

namespace Haggly.Infrastructure.Persistence.Repositories.Markets;

public sealed class EfStallCommandRepository(HagglyDbContext dbContext)
    : IStallCommandRepository
{
    public Task<bool> CodeExistsAsync(
        Guid marketId,
        string code,
        Guid? excludingId,
        CancellationToken cancellationToken)
        => dbContext.Stalls.AnyAsync(
            stall => stall.MarketId == marketId
                && stall.Code == code
                && (excludingId == null || stall.Id != excludingId),
            cancellationToken);

    public Task<bool> MarketExistsAsync(Guid marketId, CancellationToken cancellationToken)
        => dbContext.Markets.AnyAsync(market => market.Id == marketId, cancellationToken);

    public Task<bool> VendorExistsAsync(Guid vendorId, CancellationToken cancellationToken)
        => dbContext.VendorProfiles.AnyAsync(vendor => vendor.UserId == vendorId, cancellationToken);

    public Task<Stall?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Stalls.SingleOrDefaultAsync(stall => stall.Id == id, cancellationToken);

    public Task AddAsync(Stall stall, CancellationToken cancellationToken)
    {
        dbContext.Stalls.Add(stall);
        return Task.CompletedTask;
    }

    public Task AddInventoryAsync(DomainInventory inventory, CancellationToken cancellationToken)
    {
        dbContext.Inventories.Add(inventory);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
