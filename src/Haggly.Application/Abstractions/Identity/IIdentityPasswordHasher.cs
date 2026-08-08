using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Abstractions.Identity;

public interface IIdentityPasswordHasher
{
    string Hash(User user, string password);
}
