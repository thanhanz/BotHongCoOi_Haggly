using Haggly.Application.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Abstractions.Sales;

public interface IOrderQuery
{
    Task<PagedResult<Order>> GetPageAsync(
        Guid buyerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
}
