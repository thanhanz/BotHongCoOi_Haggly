using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Abstractions.Identity;

public interface IIdentityRegistrationRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task<Role?> FindActiveRoleAsync(
        RoleCode roleCode,
        CancellationToken cancellationToken);

    Task SaveRegistrationAsync(
        User user,
        UserRole userRole,
        BuyerProfile? buyerProfile,
        VendorProfile? vendorProfile,
        CancellationToken cancellationToken);
}
