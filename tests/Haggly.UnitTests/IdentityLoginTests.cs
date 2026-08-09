using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Login.Commands;
using Haggly.Application.Modules.Identity.Login.Dtos;
using Haggly.Application.Modules.Identity.Login.Exceptions;
using Haggly.Application.Modules.Identity.Login;
using Haggly.Domain.Modules.Identity;
using Xunit;

namespace Haggly.UnitTests;

public sealed class IdentityLoginTests
{
    [Fact]
    public async Task Login_returns_access_token_and_active_roles_for_an_active_user()
    {
        var user = new User
        {
            Email = "buyer@example.com",
            PasswordHash = "stored-hash",
            Status = UserStatus.ACTIVE
        };
        var repository = new RecordingLoginRepository
        {
            User = user,
            Roles = [RoleCode.BUYER]
        };
        var tokenService = new FixedTokenService();
        var handler = new LoginHandler(repository, new SuccessfulPasswordVerifier(), tokenService);

        var result = await handler.HandleAsync(
            new LoginCommand("buyer@example.com", "password-123"),
            CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("buyer@example.com", result.Email);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal([RoleCode.BUYER], result.Roles);
        Assert.NotNull(user.LastLoginAt);
        Assert.True(repository.LastLoginSaved);
        Assert.True(tokenService.WasCalled);
    }

    [Fact]
    public async Task Login_rejects_invalid_password_without_updating_last_login()
    {
        var user = new User
        {
            Email = "buyer@example.com",
            PasswordHash = "stored-hash",
            Status = UserStatus.ACTIVE
        };
        var repository = new RecordingLoginRepository { User = user };
        var handler = new LoginHandler(repository, new FailedPasswordVerifier(), new FixedTokenService());

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            handler.HandleAsync(
                new LoginCommand("buyer@example.com", "wrong-password"),
                CancellationToken.None));

        Assert.Null(user.LastLoginAt);
        Assert.False(repository.LastLoginSaved);
    }

    [Theory]
    [InlineData(UserStatus.PENDING)]
    [InlineData(UserStatus.SUSPENDED)]
    public async Task Login_rejects_users_who_are_not_active(UserStatus status)
    {
        var user = new User
        {
            Email = "user@example.com",
            PasswordHash = "stored-hash",
            Status = status
        };
        var repository = new RecordingLoginRepository { User = user };
        var handler = new LoginHandler(repository, new SuccessfulPasswordVerifier(), new FixedTokenService());

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            handler.HandleAsync(
                new LoginCommand("user@example.com", "password-123"),
                CancellationToken.None));

        Assert.False(repository.LastLoginSaved);
    }

    [Fact]
    public async Task Login_uses_a_generic_error_when_the_email_is_unknown()
    {
        var repository = new RecordingLoginRepository();
        var handler = new LoginHandler(repository, new SuccessfulPasswordVerifier(), new FixedTokenService());

        var exception = await Assert.ThrowsAsync<AuthenticationException>(() =>
            handler.HandleAsync(
                new LoginCommand("unknown@example.com", "password-123"),
                CancellationToken.None));

        Assert.Equal("Invalid email or password.", exception.Message);
    }

    [Fact]
    public async Task Login_rejects_invalid_input_before_querying_the_repository()
    {
        var repository = new RecordingLoginRepository();
        var handler = new LoginHandler(repository, new SuccessfulPasswordVerifier(), new FixedTokenService());

        await Assert.ThrowsAsync<LoginValidationException>(() =>
            handler.HandleAsync(new LoginCommand("", ""), CancellationToken.None));

        Assert.False(repository.WasQueried);
    }

    private sealed class SuccessfulPasswordVerifier : IPasswordHasher
    {
        public string Hash(User user, string password) => "unused";

        public bool Verify(User user, string password, string passwordHash) => true;
    }

    private sealed class FailedPasswordVerifier : IPasswordHasher
    {
        public string Hash(User user, string password) => "unused";

        public bool Verify(User user, string password, string passwordHash) => false;
    }

    private sealed class FixedTokenService : IIdentityTokenService
    {
        public bool WasCalled { get; private set; }

        public IssuedAccessToken CreateAccessToken(
            User user,
            IReadOnlyCollection<RoleCode> roles)
        {
            WasCalled = true;
            return new IssuedAccessToken("access-token", DateTimeOffset.UtcNow.AddMinutes(15));
        }
    }

    private sealed class RecordingLoginRepository : IIdentityLoginRepository
    {
        public User? User { get; init; }
        public IReadOnlyCollection<RoleCode> Roles { get; init; } = [];
        public bool WasQueried { get; private set; }
        public bool LastLoginSaved { get; private set; }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        {
            WasQueried = true;
            return Task.FromResult(User);
        }

        public Task<IReadOnlyCollection<RoleCode>> GetActiveRoleCodesAsync(
            Guid userId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
            => Task.FromResult(Roles);

        public Task SaveLastLoginAsync(User user, CancellationToken cancellationToken)
        {
            LastLoginSaved = true;
            return Task.CompletedTask;
        }
    }
}
