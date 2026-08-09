using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Login.Commands;
using Haggly.Application.Modules.Identity.Login.Dtos;
using Haggly.Application.Modules.Identity.Login.Exceptions;
using Haggly.Application.Modules.Identity.Login.Validation;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Modules.Identity.Login;

public sealed class LoginHandler(
    IIdentityLoginRepository repository,
    IPasswordHasher passwordHasher,
    IIdentityTokenService tokenService) : ILoginUseCase
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<LoginResult> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        LoginValidation.Validate(command.Email, command.Password);

        var user = await repository.FindByEmailAsync(command.Email, cancellationToken);
        if (user is null
            || user.Status != UserStatus.ACTIVE
            || !passwordHasher.Verify(user, command.Password, user.PasswordHash))
        {
            throw new AuthenticationException(InvalidCredentialsMessage);
        }

        var now = DateTimeOffset.UtcNow;
        var roles = await repository.GetActiveRoleCodesAsync(user.Id, now, cancellationToken);
        if (roles.Count == 0)
            throw new AuthenticationException(InvalidCredentialsMessage);

        user.LastLoginAt = now;
        await repository.SaveLastLoginAsync(user, cancellationToken);

        var token = tokenService.CreateAccessToken(user, roles);
        return new LoginResult(
            user.Id,
            user.Email,
            token.Value,
            "Bearer",
            token.ExpiresAt,
            roles);
    }
}
