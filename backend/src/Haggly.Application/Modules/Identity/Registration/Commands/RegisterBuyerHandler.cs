using Haggly.Application.Abstractions.Identity;
using Haggly.Application.Modules.Identity.Registration.Commands;
using Haggly.Application.Modules.Identity.Registration.Dtos;
using Haggly.Application.Modules.Identity.Registration.Exceptions;
using Haggly.Application.Modules.Identity.Registration.Validation;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Application.Modules.Identity.Registration.Commands;

public sealed class RegisterBuyerHandler(
    IIdentityRegistrationRepository repository,
    IPasswordHasher passwordHasher) : IRegisterBuyerUseCase
{
    public async Task<RegistrationResult> HandleAsync(
        RegisterBuyerCommand command,
        CancellationToken cancellationToken)
    {
        RegistrationValidation.ValidateCommon(
            command.Email,
            command.PhoneNumber,
            command.Password,
            command.FullName);

        return await RegisterAsync(
            command.Email,
            command.PhoneNumber,
            command.Password,
            command.FullName,
            repository,
            passwordHasher,
            cancellationToken);
    }

    internal static async Task<RegistrationResult> RegisterAsync(
        string email,
        string phoneNumber,
        string password,
        string fullName,
        IIdentityRegistrationRepository repository,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        if (await repository.EmailExistsAsync(email, cancellationToken))
            throw new RegistrationConflictException("An account with this email already exists.");

        var role = await repository.FindActiveRoleAsync(RoleCode.BUYER, cancellationToken)
            ?? throw new InvalidOperationException("The BUYER role is not configured.");

        var user = new User
        {
            Email = email,
            PhoneNumber = phoneNumber,
            FullName = fullName,
            Status = UserStatus.ACTIVE
        };
        user.PasswordHash = passwordHasher.Hash(user, password);

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            IsActive = true
        };

        var buyerProfile = new BuyerProfile { UserId = user.Id };
        await repository.SaveRegistrationAsync(user, userRole, buyerProfile, null, cancellationToken);

        return new RegistrationResult(user.Id, user.Email, user.Status, role.Code);
    }
}
