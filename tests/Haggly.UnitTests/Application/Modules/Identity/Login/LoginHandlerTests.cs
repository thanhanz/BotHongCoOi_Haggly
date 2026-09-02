using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Login.Commands;
using Haggly.Application.Modules.Identity.Login.Dtos;
using Haggly.Application.Modules.Identity.Login.Exceptions;
using Haggly.Domain.Modules.Identity;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Identity.Login;

public sealed class LoginHandlerTests
{
    private readonly IIdentityLoginRepository _repository = Substitute.For<IIdentityLoginRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IIdentityTokenService _tokenService = Substitute.For<IIdentityTokenService>();

    [Fact]
    public async Task HandleAsync_ActiveUserWithValidCredentials_ReturnsTokenAndSavesLogin()
    {
        // Arrange
        var user = new User { Email = "buyer@example.com", PasswordHash = "stored", Status = UserStatus.ACTIVE };
        _repository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(user, "password-123", user.PasswordHash).Returns(true);
        _repository.GetActiveRoleCodesAsync(user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns<IReadOnlyCollection<RoleCode>>([RoleCode.BUYER]);
        _tokenService.CreateAccessToken(user, Arg.Any<IReadOnlyCollection<RoleCode>>()).Returns(new IssuedAccessToken("access-token", DateTimeOffset.Parse("2026-08-30T10:00:00+00:00")));

        // Act
        var result = await CreateSubject().HandleAsync(new LoginCommand(user.Email, "password-123"), CancellationToken.None);

        // Assert
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal([RoleCode.BUYER], result.Roles);
        Assert.NotNull(user.LastLoginAt);
        await _repository.Received(1).SaveLastLoginAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidPassword_ThrowsAuthenticationWithoutSavingLogin()
    {
        // Arrange
        var user = new User { Email = "buyer@example.com", PasswordHash = "stored", Status = UserStatus.ACTIVE };
        _repository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(user, "wrong", user.PasswordHash).Returns(false);

        // Act
        var action = () => CreateSubject().HandleAsync(new LoginCommand(user.Email, "wrong"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<AuthenticationException>(action);
        await _repository.DidNotReceive().SaveLastLoginAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        _tokenService.DidNotReceive().CreateAccessToken(Arg.Any<User>(), Arg.Any<IReadOnlyCollection<RoleCode>>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task HandleAsync_InvalidEmail_ThrowsValidationBeforeQuery(string email)
    {
        // Arrange
        var command = new LoginCommand(email, "password-123");

        // Act
        var action = () => CreateSubject().HandleAsync(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<LoginValidationException>(action);
        await _repository.DidNotReceive().FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InactiveUser_ThrowsAuthenticationWithoutSavingLogin()
    {
        // Arrange
        var user = new User { Email = "buyer@example.com", PasswordHash = "stored", Status = UserStatus.SUSPENDED };
        _repository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var action = () => CreateSubject().HandleAsync(new LoginCommand(user.Email, "password-123"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<AuthenticationException>(action);
        await _repository.DidNotReceive().SaveLastLoginAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    private LoginHandler CreateSubject() => new(_repository, _passwordHasher, _tokenService);
}
