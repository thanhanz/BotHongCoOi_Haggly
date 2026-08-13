using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Registration;
using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
using Haggly.Domain.Modules.Identity;
using Xunit;

namespace Haggly.UnitTests.Application.Modules.Identity.Registration;

public sealed class IdentityRegistrationTests
{
    [Fact]
    public async Task Register_buyer_creates_active_user_buyer_profile_and_buyer_role()
    {
        var store = new RecordingRegistrationStore();
        var handler = new RegisterBuyerHandler(store, new FixedPasswordHasher());

        var result = await handler.HandleAsync(
            new RegisterBuyerCommand(
                Email: "buyer@example.com",
                PhoneNumber: "0900000000",
                Password: "password-123",
                FullName: "Buyer One"),
            CancellationToken.None);

        Assert.Equal(UserStatus.ACTIVE, result.Status);
        Assert.Equal(RoleCode.BUYER, result.RoleCode);
        Assert.Equal("buyer@example.com", store.User!.Email);
        Assert.Equal("hashed:password-123", store.User.PasswordHash);
        Assert.NotNull(store.BuyerProfile);
        Assert.Null(store.VendorProfile);
        Assert.Equal(RoleCode.BUYER, store.Role!.Code);
        Assert.Equal(store.User.Id, store.UserRole!.UserId);
        Assert.Equal(store.Role.Id, store.UserRole.RoleId);
    }

    [Fact]
    public async Task Register_vendor_creates_pending_user_vendor_profile_and_vendor_role()
    {
        var store = new RecordingRegistrationStore();
        var handler = new RegisterVendorHandler(store, new FixedPasswordHasher());

        var result = await handler.HandleAsync(
            new RegisterVendorCommand(
                Email: "vendor@example.com",
                PhoneNumber: "0911111111",
                Password: "password-123",
                FullName: "Vendor One",
                BusinessName: "Vendor Stall"),
            CancellationToken.None);

        Assert.Equal(UserStatus.PENDING, result.Status);
        Assert.Equal(RoleCode.VENDOR, result.RoleCode);
        Assert.Equal("vendor@example.com", store.User!.Email);
        Assert.Equal("Vendor Stall", store.VendorProfile!.BusinessName);
        Assert.Null(store.BuyerProfile);
        Assert.Equal(RoleCode.VENDOR, store.Role!.Code);
    }

    [Fact]
    public async Task Registration_rejects_an_existing_raw_email_without_writing()
    {
        var store = new RecordingRegistrationStore { EmailExists = true };
        var handler = new RegisterBuyerHandler(store, new FixedPasswordHasher());

        var exception = await Assert.ThrowsAsync<RegistrationConflictException>(() =>
            handler.HandleAsync(
                new RegisterBuyerCommand(
                    Email: "buyer@example.com",
                    PhoneNumber: "0900000000",
                    Password: "password-123",
                    FullName: "Buyer One"),
                CancellationToken.None));

        Assert.Contains("email", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(store.WasSaved);
    }

    [Fact]
    public async Task Vendor_registration_requires_a_business_name()
    {
        var store = new RecordingRegistrationStore();
        var handler = new RegisterVendorHandler(store, new FixedPasswordHasher());

        await Assert.ThrowsAsync<RegistrationValidationException>(() =>
            handler.HandleAsync(
                new RegisterVendorCommand(
                    Email: "vendor@example.com",
                    PhoneNumber: "0911111111",
                    Password: "password-123",
                    FullName: "Vendor One",
                    BusinessName: " "),
                CancellationToken.None));

        Assert.False(store.WasSaved);
    }

    [Theory]
    [InlineData("", "0900000000", "password-123", "Buyer One")]
    [InlineData("not-an-email", "0900000000", "password-123", "Buyer One")]
    [InlineData("buyer@example.com", "0900000000", "short", "Buyer One")]
    [InlineData("buyer@example.com", "0900000000", "password-123", "")]
    public async Task Buyer_registration_rejects_invalid_required_input(
        string email,
        string phoneNumber,
        string password,
        string fullName)
    {
        var store = new RecordingRegistrationStore();
        var handler = new RegisterBuyerHandler(store, new FixedPasswordHasher());

        await Assert.ThrowsAsync<RegistrationValidationException>(() =>
            handler.HandleAsync(
                new RegisterBuyerCommand(email, phoneNumber, password, fullName),
                CancellationToken.None));

        Assert.False(store.WasSaved);
    }

    private sealed class FixedPasswordHasher : IPasswordHasher
    {
        public string Hash(User user, string password) => $"hashed:{password}";

        public bool Verify(User user, string password, string passwordHash)
            => passwordHash == $"hashed:{password}";
    }

    private sealed class RecordingRegistrationStore : IIdentityRegistrationRepository
    {
        public bool EmailExists { get; init; }
        public bool WasSaved { get; private set; }
        public User? User { get; private set; }
        public Role? Role { get; private set; }
        public UserRole? UserRole { get; private set; }
        public BuyerProfile? BuyerProfile { get; private set; }
        public VendorProfile? VendorProfile { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
            => Task.FromResult(EmailExists);

        public Task<Role?> FindActiveRoleAsync(RoleCode roleCode, CancellationToken cancellationToken)
        {
            Role = new Role { Code = roleCode, Name = roleCode.ToString() };
            return Task.FromResult<Role?>(Role);
        }

        public Task SaveRegistrationAsync(
            User user,
            UserRole userRole,
            BuyerProfile? buyerProfile,
            VendorProfile? vendorProfile,
            CancellationToken cancellationToken)
        {
            WasSaved = true;
            User = user;
            UserRole = userRole;
            BuyerProfile = buyerProfile;
            VendorProfile = vendorProfile;
            return Task.CompletedTask;
        }
    }
}
