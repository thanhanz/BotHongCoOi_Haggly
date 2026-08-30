using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
using Haggly.Domain.Modules.Identity;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Identity.Registration;

public sealed class RegistrationHandlerTests
{
    private readonly IIdentityRegistrationRepository _repository = Substitute.For<IIdentityRegistrationRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    [Fact]
    public async Task HandleAsync_ValidBuyerRegistration_CreatesActiveBuyer()
    {
        // Arrange
        var role = new Role { Code = RoleCode.BUYER, Name = "Buyer" };
        ConfigureRole(role);
        _passwordHasher.Hash(Arg.Any<User>(), "password-123").Returns("hashed-password");
        var command = new RegisterBuyerCommand("buyer@example.com", "0900000000", "password-123", "Buyer One");

        // Act
        var result = await new RegisterBuyerHandler(_repository, _passwordHasher).HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(UserStatus.ACTIVE, result.Status);
        Assert.Equal(RoleCode.BUYER, result.RoleCode);
        await _repository.Received(1).SaveRegistrationAsync(
            Arg.Is<User>(user => user.Email == command.Email && user.PasswordHash == "hashed-password"),
            Arg.Is<UserRole>(userRole => userRole.RoleId == role.Id),
            Arg.Is<BuyerProfile>(profile => profile.UserId != Guid.Empty),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidVendorRegistration_CreatesPendingVendor()
    {
        // Arrange
        var role = new Role { Code = RoleCode.VENDOR, Name = "Vendor" };
        ConfigureRole(role);
        _passwordHasher.Hash(Arg.Any<User>(), "password-123").Returns("hashed-password");
        var command = new RegisterVendorCommand("vendor@example.com", "0911111111", "password-123", "Vendor One", "Vendor Stall");

        // Act
        var result = await new RegisterVendorHandler(_repository, _passwordHasher).HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(UserStatus.PENDING, result.Status);
        Assert.Equal(RoleCode.VENDOR, result.RoleCode);
        await _repository.Received(1).SaveRegistrationAsync(
            Arg.Is<User>(user => user.Email == command.Email),
            Arg.Any<UserRole>(),
            null,
            Arg.Is<VendorProfile>(profile => profile.BusinessName == "Vendor Stall"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailAlreadyExists_ThrowsConflictWithoutSaving()
    {
        // Arrange
        _repository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var command = new RegisterBuyerCommand("buyer@example.com", "0900000000", "password-123", "Buyer One");

        // Act
        var action = () => new RegisterBuyerHandler(_repository, _passwordHasher).HandleAsync(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RegistrationConflictException>(action);
        await _repository.DidNotReceive().SaveRegistrationAsync(
            Arg.Any<User>(), Arg.Any<UserRole>(), Arg.Any<BuyerProfile?>(), Arg.Any<VendorProfile?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidVendorBusinessName_ThrowsValidationWithoutQuery()
    {
        // Arrange
        var command = new RegisterVendorCommand("vendor@example.com", "0911111111", "password-123", "Vendor One", " ");

        // Act
        var action = () => new RegisterVendorHandler(_repository, _passwordHasher).HandleAsync(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RegistrationValidationException>(action);
        await _repository.DidNotReceive().EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private void ConfigureRole(Role role)
    {
        _repository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.FindActiveRoleAsync(role.Code, Arg.Any<CancellationToken>()).Returns(role);
    }
}
