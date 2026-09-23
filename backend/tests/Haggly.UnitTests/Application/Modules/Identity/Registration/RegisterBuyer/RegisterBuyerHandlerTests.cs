using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
using Haggly.Domain.Modules.Identity;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Identity.Registration.RegisterBuyer;

public sealed class RegisterBuyerHandlerTests
{
    private readonly IIdentityRegistrationRepository _repository = Substitute.For<IIdentityRegistrationRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    [Fact]
    public async Task HandleAsync_ValidBuyerRegistration_CreatesActiveBuyerAndSavesRegistration()
    {
        // Arrange
        var role = new Role { Code = RoleCode.BUYER, Name = "Buyer" };
        ConfigureRole(role);
        _passwordHasher.Hash(Arg.Any<User>(), "password-123").Returns("hashed-password");
        var command = new RegisterBuyerCommand("buyer@example.com", "0900000000", "password-123", "Buyer One");

        // Act
        var result = await CreateSubject().HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(UserStatus.ACTIVE, result.Status);
        Assert.Equal(RoleCode.BUYER, result.RoleCode);
        _passwordHasher.Received(1).Hash(
            Arg.Is<User>(user => user.Status == UserStatus.ACTIVE), "password-123");
        await _repository.Received(1).SaveRegistrationAsync(
            Arg.Is<User>(user => user.Email == command.Email && user.PasswordHash == "hashed-password"),
            Arg.Is<UserRole>(userRole => userRole.RoleId == role.Id && userRole.IsActive),
            Arg.Is<BuyerProfile>(profile => profile.UserId != Guid.Empty),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ThrowsConflictWithoutSaving()
    {
        // Arrange
        _repository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var action = () => CreateSubject().HandleAsync(
            new RegisterBuyerCommand("buyer@example.com", "0900000000", "password-123", "Buyer One"),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RegistrationConflictException>(action);
        await _repository.DidNotReceive().SaveRegistrationAsync(
            Arg.Any<User>(), Arg.Any<UserRole>(), Arg.Any<BuyerProfile?>(), Arg.Any<VendorProfile?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidBuyerInput_ThrowsValidationWithoutQueryOrSave()
    {
        // Arrange
        var command = new RegisterBuyerCommand(" ", "0900000000", "password-123", "Buyer One");

        // Act
        var action = () => CreateSubject().HandleAsync(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RegistrationValidationException>(action);
        await _repository.DidNotReceive().EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveRegistrationAsync(
            Arg.Any<User>(), Arg.Any<UserRole>(), Arg.Any<BuyerProfile?>(), Arg.Any<VendorProfile?>(),
            Arg.Any<CancellationToken>());
    }

    private RegisterBuyerHandler CreateSubject() => new(_repository, _passwordHasher);

    private void ConfigureRole(Role role)
    {
        _repository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.FindActiveRoleAsync(RoleCode.BUYER, Arg.Any<CancellationToken>()).Returns(role);
    }
}
