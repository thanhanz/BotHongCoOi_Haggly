using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Abstractions.Identity;

public sealed record VendorAdminAggregate(User User, VendorProfile VendorProfile);

public interface IVendorAdminCommandRepository
{
    Task<VendorAdminAggregate?> FindByIdAsync(
        Guid vendorId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
