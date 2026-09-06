using Haggly.Application.Abstractions.Inventory;
using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Inventory;

public sealed class EfInventoryReferenceQuery(HagglyDbContext dbContext)
    : IInventoryReferenceQuery
{
    public Task<Stall?> FindActiveStallAsync(
        Guid stallId,
        CancellationToken cancellationToken)
        => dbContext.Stalls.SingleOrDefaultAsync(
            stall => stall.Id == stallId,
            cancellationToken);

    public Task<ProductStall?> FindActiveProductStallAsync(
        Guid stallId,
        Guid productStallId,
        CancellationToken cancellationToken)
        => dbContext.ProductStalls
            .Include(productStall => productStall.Product)
            .SingleOrDefaultAsync(
                productStall => productStall.Id == productStallId
                    && productStall.StallId == stallId,
                cancellationToken);
}
