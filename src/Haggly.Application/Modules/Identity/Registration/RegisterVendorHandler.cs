using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Registration.Dtos;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
using Haggly.Application.Modules.Identity.Registration.Validation;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Modules.Identity.Registration;

public sealed class RegisterVendorHandler(
    IIdentityRegistrationRepository repository,
    IIdentityPasswordHasher passwordHasher) : IRegisterVendorUseCase
{
    public async Task<RegistrationResult> HandleAsync(
        RegisterVendorCommand command,
        CancellationToken cancellationToken)
    {
        RegistrationValidation.ValidateCommon(
            command.Email,
            command.PhoneNumber,
            command.Password,
            command.FullName);
        RegistrationValidation.ValidateVendor(command.BusinessName);

        if (await repository.EmailExistsAsync(command.Email, cancellationToken))
            throw new RegistrationConflictException("An account with this email already exists.");

        var role = await repository.FindActiveRoleAsync(RoleCode.VENDOR, cancellationToken)
            ?? throw new InvalidOperationException("The VENDOR role is not configured.");

        var user = new User
        {
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            FullName = command.FullName,
            Status = UserStatus.PENDING
        };
        user.PasswordHash = passwordHasher.Hash(user, command.Password);

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            IsActive = true
        };

        var vendorProfile = new VendorProfile
        {
            UserId = user.Id,
            BusinessName = command.BusinessName,
            BusinessRegistrationNo = command.BusinessRegistrationNo,
            TaxCode = command.TaxCode,
            ApprovalStatus = ApprovalStatus.PENDING
        };

        await repository.SaveRegistrationAsync(user, userRole, null, vendorProfile, cancellationToken);

        return new RegistrationResult(user.Id, user.Email, user.Status, role.Code);
    }
}
