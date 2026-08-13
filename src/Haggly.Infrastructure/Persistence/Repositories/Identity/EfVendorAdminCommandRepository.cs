using Haggly.Application.Abstractions.Identity;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Identity;

public sealed class EfVendorAdminCommandRepository(HagglyDbContext dbContext)
    : IVendorAdminCommandRepository
{
    public async Task<VendorAdminAggregate?> FindByIdAsync(
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(candidate => candidate.VendorProfile)
            .SingleOrDefaultAsync(candidate => candidate.Id == vendorId, cancellationToken);

        return user?.VendorProfile is { } vendor
            ? new VendorAdminAggregate(user, vendor)
            : null;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
