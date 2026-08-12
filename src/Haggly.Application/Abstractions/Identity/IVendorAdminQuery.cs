using Haggly.Application.Common;
using Haggly.Application.Modules.Identity.Administration;
using Haggly.Application.Modules.Identity.Dtos;

namespace Haggly.Application.Abstractions.Identity;

public interface IVendorAdminQuery
{
    Task<PagedResult<VendorAdminDto>> GetPageAsync(
        VendorListFilter filter,
        CancellationToken cancellationToken);
}
