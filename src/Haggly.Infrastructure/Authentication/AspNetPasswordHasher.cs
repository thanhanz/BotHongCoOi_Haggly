using Haggly.Application.Abstractions.Identity;
using Haggly.Domain.Modules.Identity;
using Microsoft.AspNetCore.Identity;

namespace Haggly.Infrastructure.Authentication;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string Hash(User user, string password)
        => _passwordHasher.HashPassword(user, password);

    public bool Verify(User user, string password, string passwordHash)
        => _passwordHasher.VerifyHashedPassword(user, passwordHash, password)
            != PasswordVerificationResult.Failed;
}
