using Haggly.Application.Abstractions.Identity;
using Haggly.Domain.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace Haggly.Infrastructure.Persistence.Repositories.Identity;

public sealed class EfIdentityLoginRepository(HagglyDbContext dbContext)
    : IIdentityLoginRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        => dbContext.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

  public async Task<IReadOnlyCollection<RoleCode>> GetActiveRoleCodesAsync(
      Guid userId,
      DateTimeOffset now,
      CancellationToken cancellationToken)
      => await dbContext.UserRoles
          .Where(userRole =>
              userRole.UserId == userId
              && userRole.IsActive
              && (userRole.ExpiresAt == null || userRole.ExpiresAt > now))
          .Join(
              dbContext.Roles,
              userRole => userRole.RoleId,
              role => role.Id,
              (_, role) => role)
          .Where(role => role.IsActive)
          .Select(role => role.Code)
          .ToListAsync(cancellationToken);


  public async Task SaveLastLoginAsync(
        User user,
        CancellationToken cancellationToken)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
