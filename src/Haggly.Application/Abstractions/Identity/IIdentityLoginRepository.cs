using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Abstractions.Identity;

public interface IIdentityLoginRepository
{
    Task<User?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RoleCode>> GetActiveRoleCodesAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task SaveLastLoginAsync(
        User user,
        CancellationToken cancellationToken);
}
