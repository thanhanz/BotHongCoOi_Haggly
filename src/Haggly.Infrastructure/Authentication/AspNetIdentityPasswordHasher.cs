using Haggly.Application.Abstractions.Identity;
using Haggly.Domain.Modules.Identity;
using Microsoft.AspNetCore.Identity;

namespace Haggly.Infrastructure.Authentication;

public sealed class AspNetIdentityPasswordHasher : IIdentityPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string Hash(User user, string password)
        => _passwordHasher.HashPassword(user, password);
}
