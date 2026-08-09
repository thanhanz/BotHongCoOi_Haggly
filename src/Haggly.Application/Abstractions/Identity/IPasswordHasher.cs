using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Abstractions.Identity;

public interface IPasswordHasher
{
    string Hash(User user, string password);

    bool Verify(User user, string password, string passwordHash);
}
