using Haggly.Domain.Modules.Catalog;
using Haggly.Domain.Modules.Markets;

namespace Haggly.Application.Abstractions.Inventory;

public interface IInventoryReferenceQuery
{
    Task<Stall?> FindActiveStallAsync(
        Guid stallId,
        CancellationToken cancellationToken);

    Task<ProductStall?> FindActiveProductStallAsync(
        Guid stallId,
        Guid productStallId,
        CancellationToken cancellationToken);
}
