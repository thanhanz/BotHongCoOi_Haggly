using Haggly.Application.Abstractions.Identity;
using Haggly.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Identity;

public sealed class EfIdentityRegistrationRepository(HagglyDbContext dbContext)
    : IIdentityRegistrationRepository
{
    public Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken)
        => dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public Task<Role?> FindActiveRoleAsync(
        RoleCode roleCode,
        CancellationToken cancellationToken)
        => dbContext.Roles.SingleOrDefaultAsync(
            role => role.Code == roleCode && role.IsActive,
            cancellationToken);

    public async Task SaveRegistrationAsync(
        User user,
        UserRole userRole,
        BuyerProfile? buyerProfile,
        VendorProfile? vendorProfile,
        CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        dbContext.UserRoles.Add(userRole);

        if (buyerProfile is not null)
            dbContext.BuyerProfiles.Add(buyerProfile);

        if (vendorProfile is not null)
            dbContext.VendorProfiles.Add(vendorProfile);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
