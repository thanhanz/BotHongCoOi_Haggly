using Haggly.Application.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Application.Abstractions.Sales;

public interface IPosSaleQuery
{
    Task<PagedResult<PosSale>> GetPageAsync(
        Guid stallId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
