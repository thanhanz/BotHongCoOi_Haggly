using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
using Haggly.Domain.Modules.Identity;
using NSubstitute;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Identity.Registration.RegisterVendor;

public sealed class RegisterVendorHandlerTests
{
    private readonly IIdentityRegistrationRepository _repository = Substitute.For<IIdentityRegistrationRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    [Fact]
    public async Task HandleAsync_ValidVendorRegistration_CreatesPendingVendorAndSavesRegistration()
    {
        // Arrange
        var role = new Role { Code = RoleCode.VENDOR, Name = "Vendor" };
        ConfigureRole(role);
        _passwordHasher.Hash(Arg.Any<User>(), "password-123").Returns("hashed-password");
        var command = new RegisterVendorCommand(
            "vendor@example.com", "0911111111", "password-123", "Vendor One", "Vendor Stall",
            "REG-001", "TAX-001");

        // Act
        var result = await CreateSubject().HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(UserStatus.PENDING, result.Status);
        Assert.Equal(RoleCode.VENDOR, result.RoleCode);
        _passwordHasher.Received(1).Hash(
            Arg.Is<User>(user => user.Status == UserStatus.PENDING), "password-123");
        await _repository.Received(1).SaveRegistrationAsync(
            Arg.Is<User>(user => user.PasswordHash == "hashed-password"),
            Arg.Is<UserRole>(userRole => userRole.RoleId == role.Id && userRole.IsActive),
            null,
            Arg.Is<VendorProfile>(profile =>
                profile.BusinessName == "Vendor Stall" &&
                profile.BusinessRegistrationNo == "REG-001" &&
                profile.TaxCode == "TAX-001" &&
                profile.ApprovalStatus == ApprovalStatus.PENDING),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidBusinessName_ThrowsValidationWithoutQueryOrSave()
    {
        // Arrange
        var command = new RegisterVendorCommand(
            "vendor@example.com", "0911111111", "password-123", "Vendor One", " ");

        // Act
        var action = () => CreateSubject().HandleAsync(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RegistrationValidationException>(action);
        await _repository.DidNotReceive().EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveRegistrationAsync(
            Arg.Any<User>(), Arg.Any<UserRole>(), Arg.Any<BuyerProfile?>(), Arg.Any<VendorProfile?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ThrowsConflictWithoutSaving()
    {
        // Arrange
        _repository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var command = new RegisterVendorCommand(
            "vendor@example.com", "0911111111", "password-123", "Vendor One", "Vendor Stall");

        // Act
        var action = () => CreateSubject().HandleAsync(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<RegistrationConflictException>(action);
        await _repository.DidNotReceive().SaveRegistrationAsync(
            Arg.Any<User>(), Arg.Any<UserRole>(), Arg.Any<BuyerProfile?>(), Arg.Any<VendorProfile?>(),
            Arg.Any<CancellationToken>());
    }

    private RegisterVendorHandler CreateSubject() => new(_repository, _passwordHasher);

    private void ConfigureRole(Role role)
    {
        _repository.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _repository.FindActiveRoleAsync(RoleCode.VENDOR, Arg.Any<CancellationToken>()).Returns(role);
    }
}
